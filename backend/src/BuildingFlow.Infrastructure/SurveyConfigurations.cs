using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace BuildingFlow.Infrastructure;
// Anketler: every child row carries property_id and a composite FK, so a row can never point into another management area.
internal sealed class SurveyConfiguration : IEntityTypeConfiguration<Survey>
{
    public void Configure(EntityTypeBuilder<Survey> b)
    {
        BaseEntityConfiguration.Configure(b, "surveys");
        b.Property(x => x.PropertyId).HasColumnName("property_id");
        b.Property(x => x.Title).HasColumnName("title").HasMaxLength(Survey.TitleLength);
        b.Property(x => x.Description).HasColumnName("description").HasMaxLength(Survey.DescriptionLength);
        b.Property(x => x.Category).HasColumnName("category").HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.StartDate).HasColumnName("start_date");
        b.Property(x => x.EndDate).HasColumnName("end_date");
        b.Property(x => x.Audience).HasColumnName("audience").HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.ResponseMode).HasColumnName("response_mode").HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.IsAnonymous).HasColumnName("is_anonymous");
        b.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.PublishedAt).HasColumnName("published_at");
        b.Property(x => x.ClosedAt).HasColumnName("closed_at");
        b.Property(x => x.ResponseCount).HasColumnName("response_count");
        b.Property(x => x.EligibleCount).HasColumnName("eligible_count");
        b.HasOne<Property>().WithMany().HasForeignKey(x => x.PropertyId).OnDelete(DeleteBehavior.Restrict);
        b.HasAlternateKey(x => new { x.PropertyId, x.Id });
        b.HasMany(x => x.Questions).WithOne().HasForeignKey(x => new { x.PropertyId, x.SurveyId }).HasPrincipalKey(x => new { x.PropertyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.Navigation(x => x.Questions).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.HasIndex(x => new { x.PropertyId, x.StartDate });
        b.ToTable(t => {
            t.HasCheckConstraint("ck_surveys_valid", "category IN ('DUES','SERVICE','SECURITY','SOCIAL','PARKING','ENVIRONMENT','GENERAL') AND audience IN ('ALL_RESIDENTS','OWNERS','TENANTS') AND response_mode IN ('PER_UNIT','PER_ACCOUNT') AND length(trim(title)) > 0 AND start_date BETWEEN DATE '2000-01-01' AND DATE '2100-12-31' AND end_date BETWEEN start_date AND DATE '2100-12-31' AND response_count >= 0 AND (eligible_count IS NULL OR eligible_count >= 0)");
            t.HasCheckConstraint("ck_surveys_status", "(status = 'DRAFT' AND published_at IS NULL AND closed_at IS NULL AND response_count = 0) OR (status = 'PUBLISHED' AND published_at IS NOT NULL AND closed_at IS NULL) OR (status = 'CLOSED' AND published_at IS NOT NULL AND closed_at IS NOT NULL AND eligible_count IS NOT NULL)");
        });
    }
}
internal sealed class SurveyQuestionConfiguration : IEntityTypeConfiguration<SurveyQuestion>
{
    public void Configure(EntityTypeBuilder<SurveyQuestion> b)
    {
        BaseEntityConfiguration.Configure(b, "survey_questions");
        b.Property(x => x.SurveyId).HasColumnName("survey_id");
        b.Property(x => x.PropertyId).HasColumnName("property_id");
        b.Property(x => x.Position).HasColumnName("position");
        b.Property(x => x.Text).HasColumnName("text").HasMaxLength(SurveyQuestion.TextLength);
        b.Property(x => x.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.IsRequired).HasColumnName("is_required");
        b.HasAlternateKey(x => new { x.PropertyId, x.Id });
        b.HasMany(x => x.Options).WithOne().HasForeignKey(x => new { x.PropertyId, x.QuestionId }).HasPrincipalKey(x => new { x.PropertyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.Navigation(x => x.Options).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.HasIndex(x => new { x.SurveyId, x.Position }).IsUnique().HasFilter("is_deleted = false");
        b.ToTable(t => t.HasCheckConstraint("ck_survey_questions_valid", "type IN ('SINGLE_CHOICE','MULTIPLE_CHOICE','RATING','TEXT') AND position BETWEEN 1 AND 30 AND length(trim(text)) > 0"));
    }
}
internal sealed class SurveyOptionConfiguration : IEntityTypeConfiguration<SurveyOption>
{
    public void Configure(EntityTypeBuilder<SurveyOption> b)
    {
        BaseEntityConfiguration.Configure(b, "survey_options");
        b.Property(x => x.QuestionId).HasColumnName("question_id");
        b.Property(x => x.SurveyId).HasColumnName("survey_id");
        b.Property(x => x.PropertyId).HasColumnName("property_id");
        b.Property(x => x.Position).HasColumnName("position");
        b.Property(x => x.Text).HasColumnName("text").HasMaxLength(SurveyOption.TextLength);
        b.HasAlternateKey(x => new { x.PropertyId, x.Id });
        b.HasOne<Survey>().WithMany().HasForeignKey(x => new { x.PropertyId, x.SurveyId }).HasPrincipalKey(x => new { x.PropertyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.QuestionId, x.Position }).IsUnique().HasFilter("is_deleted = false");
        b.ToTable(t => t.HasCheckConstraint("ck_survey_options_valid", "position BETWEEN 1 AND 10 AND length(trim(text)) > 0"));
    }
}
internal sealed class SurveyParticipationConfiguration : IEntityTypeConfiguration<SurveyParticipation>
{
    public void Configure(EntityTypeBuilder<SurveyParticipation> b)
    {
        BaseEntityConfiguration.Configure(b, "survey_participations");
        b.Property(x => x.SurveyId).HasColumnName("survey_id");
        b.Property(x => x.PropertyId).HasColumnName("property_id");
        b.Property(x => x.BallotKey).HasColumnName("ballot_key").HasMaxLength(SurveyParticipation.KeyLength);
        b.Property(x => x.UnitId).HasColumnName("unit_id");
        b.Property(x => x.RespondentUserId).HasColumnName("respondent_user_id").HasMaxLength(200);
        b.Property(x => x.Role).HasColumnName("role").HasConversion<string>().HasMaxLength(20);
        b.HasOne<Survey>().WithMany().HasForeignKey(x => new { x.PropertyId, x.SurveyId }).HasPrincipalKey(x => new { x.PropertyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Unit>().WithMany().HasForeignKey(x => new { x.PropertyId, x.UnitId }).HasPrincipalKey(x => new { x.PropertyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        // One answer per ballot is enforced by the database too; participation rows are never deleted.
        b.HasIndex(x => new { x.SurveyId, x.BallotKey }).IsUnique();
        b.HasIndex(x => new { x.SurveyId, x.RespondentUserId });
        b.ToTable(t => t.HasCheckConstraint("ck_survey_participations_valid", "role IN ('OWNER','TENANT','RESIDENT') AND (ballot_key LIKE 'UNIT:%' OR ballot_key LIKE 'ACCOUNT:%')"));
    }
}
internal sealed class SurveyResponseConfiguration : IEntityTypeConfiguration<SurveyResponse>
{
    public void Configure(EntityTypeBuilder<SurveyResponse> b)
    {
        BaseEntityConfiguration.Configure(b, "survey_responses");
        b.Property(x => x.SurveyId).HasColumnName("survey_id");
        b.Property(x => x.PropertyId).HasColumnName("property_id");
        b.Property(x => x.ParticipationId).HasColumnName("participation_id");
        b.Property(x => x.UnitId).HasColumnName("unit_id");
        b.Property(x => x.SubmittedOn).HasColumnName("submitted_on");
        b.Property(x => x.IsAnonymous).HasColumnName("is_anonymous");
        b.Ignore(x => x.HideActor);
        b.HasAlternateKey(x => new { x.PropertyId, x.Id });
        b.HasOne<Survey>().WithMany().HasForeignKey(x => new { x.PropertyId, x.SurveyId }).HasPrincipalKey(x => new { x.PropertyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<SurveyParticipation>().WithMany().HasForeignKey(x => x.ParticipationId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.ParticipationId).IsUnique();
        b.HasIndex(x => new { x.SurveyId, x.SubmittedOn });
        // An anonymous response must never keep a path back to the participant.
        b.ToTable(t => t.HasCheckConstraint("ck_survey_responses_anonymous", "(is_anonymous AND participation_id IS NULL AND unit_id IS NULL AND created_by = 'anonymous' AND modified_by = 'anonymous') OR (NOT is_anonymous AND participation_id IS NOT NULL AND unit_id IS NOT NULL)"));
    }
}
internal sealed class SurveyAnswerConfiguration : IEntityTypeConfiguration<SurveyAnswer>
{
    public void Configure(EntityTypeBuilder<SurveyAnswer> b)
    {
        BaseEntityConfiguration.Configure(b, "survey_answers");
        b.Property(x => x.ResponseId).HasColumnName("response_id");
        b.Property(x => x.SurveyId).HasColumnName("survey_id");
        b.Property(x => x.PropertyId).HasColumnName("property_id");
        b.Property(x => x.QuestionId).HasColumnName("question_id");
        b.Property(x => x.OptionId).HasColumnName("option_id");
        b.Property(x => x.Rating).HasColumnName("rating");
        b.Property(x => x.Text).HasColumnName("text").HasMaxLength(SurveyAnswer.TextLength);
        b.Property(x => x.IsAnonymous).HasColumnName("is_anonymous");
        b.Ignore(x => x.HideActor);
        b.HasOne<SurveyResponse>().WithMany().HasForeignKey(x => new { x.PropertyId, x.ResponseId }).HasPrincipalKey(x => new { x.PropertyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<SurveyQuestion>().WithMany().HasForeignKey(x => new { x.PropertyId, x.QuestionId }).HasPrincipalKey(x => new { x.PropertyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<SurveyOption>().WithMany().HasForeignKey(x => new { x.PropertyId, x.OptionId }).HasPrincipalKey(x => new { x.PropertyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.SurveyId, x.QuestionId });
        b.ToTable(t => {
            t.HasCheckConstraint("ck_survey_answers_value", "num_nonnulls(option_id, rating, text) = 1 AND (rating IS NULL OR rating BETWEEN 1 AND 5) AND (text IS NULL OR length(trim(text)) > 0)");
            t.HasCheckConstraint("ck_survey_answers_anonymous", "NOT is_anonymous OR (created_by = 'anonymous' AND modified_by = 'anonymous')");
        });
    }
}
