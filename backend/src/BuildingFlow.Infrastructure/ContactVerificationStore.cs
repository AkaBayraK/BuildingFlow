using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
namespace BuildingFlow.Infrastructure;

public sealed class ContactVerificationStore(BuildingFlowDbContext db, ICurrentUserService actor, IKeycloakAdmin identity, TimeProvider clock, IPhoneOtpSender sender, IPhoneOtpSecrets secrets) : IContactVerificationStore
{
    private string User() => actor.IsAuthenticated && !string.IsNullOrWhiteSpace(actor.UserId) ? actor.UserId : throw new UnauthorizedAccessException();
    private static HierarchyConflictException Conflict() => new(ErrorCatalog.MobileSyncConflict.Create());
    private static void Active(BaseEntity? entity) { if (entity is { IsDeleted: true } or { IsEnabled: false }) throw new UnauthorizedAccessException(); }

    public async Task<ContactVerificationDto> ReadAsync(CancellationToken token)
    {
        var owner = User();
        var profile = await db.Set<MobileProfile>().IgnoreQueryFilters().AsNoTracking().SingleOrDefaultAsync(x => x.OwnerUserId == owner, token);
        var proof = await db.Set<AccountPhoneVerification>().IgnoreQueryFilters().AsNoTracking().SingleOrDefaultAsync(x => x.OwnerUserId == owner, token);
        Active(profile); Active(proof);
        EmailVerificationDto email;
        try
        {
            var user = await identity.GetAsync(owner, token);
            email = user is null ? new("UNKNOWN", null, false, clock.GetUtcNow()) :
                new(string.IsNullOrWhiteSpace(user.Email) ? "MISSING" : user.EmailVerified ? "VERIFIED" : "UNVERIFIED", user.Email, true, clock.GetUtcNow());
        }
        catch (IdentityUnavailableException) { email = new("UNKNOWN", null, false, clock.GetUtcNow()); }
        var pending = proof?.PendingPhone;
        var status = proof is null ? (string.IsNullOrWhiteSpace(profile?.Phone) ? "MISSING" : "UNVERIFIED") :
            pending is not null ? "PENDING" : proof.VerifiedKey is not null ? "VERIFIED" : "MISSING";
        var challenge=await db.Set<PhoneOtpChallenge>().IgnoreQueryFilters().AsNoTracking().Where(x=>x.OwnerUserId==owner&&!x.IsDeleted&&x.IsEnabled).OrderByDescending(x=>x.CreatedAt).FirstOrDefaultAsync(token);
        var delivery=challenge is null ? "NOT_REQUESTED" : clock.GetUtcNow()>=challenge.ExpiresAt&&!challenge.Terminal ? "EXPIRED" : challenge.Status=="RESERVED" ? "UNKNOWN" : challenge.Status;
        var candidate=proof is null ? profile?.Phone : pending;
        return new(proof?.Version ?? 0, email, new(status, profile?.Phone, proof?.VerifiedPhone,candidate,proof?.VerifiedAt,
            sender.Available&&secrets.Available&&PhoneOtpPolicy.Normalize(candidate)!=null,delivery,challenge?.Id,challenge?.ExpiresAt,
            challenge is null ? null : Math.Max(0,PhoneOtpPolicy.MaxAttempts-challenge.Attempts),challenge is null?0:Math.Max(0,(int)Math.Ceiling((challenge.ResendAfter-clock.GetUtcNow()).TotalSeconds))));
    }

    public async Task<StagePhoneResult> StagePhoneAsync(StagePhoneRequest request, CancellationToken token)
    {
        var owner = User();
        if (request.OperationId == Guid.Empty || request.ExpectedVersion < 0 || !PhoneContactKey.ValidInput(request.Phone))
            throw new RequestValidationException(new() { ["phone"] = [ErrorCatalog.UserInvalid.Create()] });
        var phone = request.Phone!.Trim();
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new {
            Purpose = "contact.stage-phone.v1", Owner = owner, request.ExpectedVersion, Phone = PhoneContactKey.FromLegacy(phone)
        }))));
        await using var tx = await db.Database.BeginTransactionAsync(token);
        // Same lock order as profile commands; legacy writers cannot race with candidate changes.
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({"profile:" + owner},0))", token);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({owner + ":" + request.OperationId},0))", token);
        var profile = await db.Set<MobileProfile>().IgnoreQueryFilters().AsNoTracking().SingleOrDefaultAsync(x => x.OwnerUserId == owner, token);
        var proof = await db.Set<AccountPhoneVerification>().IgnoreQueryFilters().SingleOrDefaultAsync(x => x.OwnerUserId == owner, token);
        Active(profile); Active(proof);
        var receipt = await db.Set<MobileOperationReceipt>().IgnoreQueryFilters().SingleOrDefaultAsync(x => x.OwnerUserId == owner && x.OperationId == request.OperationId, token);
        if (receipt is not null)
        {
            if (receipt.IsDeleted || !receipt.IsEnabled || receipt.Fingerprint != fingerprint) throw Conflict();
            await tx.CommitAsync(token);
            return new(request.OperationId, true, await ReadAsync(token));
        }
        if ((proof?.Version ?? 0) != request.ExpectedVersion) throw Conflict();
        if (proof is null) { proof = new(owner); db.Add(proof); }
        if(proof.PendingKey!=PhoneContactKey.FromLegacy(phone))await PhoneOtpStore.CancelPendingAsync(db,owner,token);
        proof.Stage(phone);
        // No OTP, provider call or claim of ownership is made in the foundation phase.
        db.Add(new MobileOperationReceipt(owner, request.OperationId, fingerprint, "{\"kind\":\"CONTACT_STAGE_PHONE\"}"));
        await db.SaveChangesAsync(token); await tx.CommitAsync(token);
        return new(request.OperationId, false, await ReadAsync(token));
    }

    // Called only inside the profile transaction, after the profile-owner lock has been acquired.
    public static async Task ObserveProfileChangeAsync(BuildingFlowDbContext db, string owner, string? before, string? after, CancellationToken token)
    {
        if (PhoneContactKey.FromLegacy(before) == PhoneContactKey.FromLegacy(after)) return;
        var proof = await db.Set<AccountPhoneVerification>().IgnoreQueryFilters().SingleOrDefaultAsync(x => x.OwnerUserId == owner, token);
        Active(proof);
        if (proof is null) { proof = new(owner); db.Add(proof); }
        if((proof.PendingKey??proof.VerifiedKey)!=PhoneContactKey.FromLegacy(after))await PhoneOtpStore.CancelPendingAsync(db,owner,token);
        proof.Stage(after);
    }
}
