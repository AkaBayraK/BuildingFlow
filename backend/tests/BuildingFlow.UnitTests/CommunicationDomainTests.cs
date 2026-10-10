using BuildingFlow.Application;
using BuildingFlow.Domain;
using Xunit;
namespace BuildingFlow.UnitTests;
public sealed class CommunicationDomainTests
{
    [Fact] public void PublicationIsOneWayAndKeepsContentHistory()
    {
        var a=new Announcement(Guid.NewGuid()," title "," content ",ContentAudience.ALL_MEMBERS);Assert.Equal("title",a.Title);Assert.Equal(AnnouncementStatus.DRAFT,a.Status);
        a.Edit("changed","body",ContentAudience.MANAGEMENT);var at=DateTimeOffset.UtcNow;a.Publish(at);Assert.Equal(at,a.PublishedAt);
        Assert.Throws<InvalidOperationException>(()=>a.Edit("rewrite","body",ContentAudience.ALL_MEMBERS));Assert.Throws<InvalidOperationException>(()=>a.Publish(at));
        a.Archive();Assert.Equal(at,a.PublishedAt);Assert.Equal("changed",a.Title);Assert.Throws<InvalidOperationException>(()=>a.Archive());
    }
    [Theory][InlineData("index.html","text/html","%PDF-1.7")][InlineData("fake.pdf","application/pdf","<script>")][InlineData("pic.svg","image/svg+xml","<svg>")][InlineData("../x.pdf","application/pdf","%PDF-1.7")][InlineData("x\r\n.pdf","application/pdf","%PDF-1.7")][InlineData("x.png","application/pdf","%PDF-1.7")]
    public void InvalidFilesDoNotPassSignatureExtensionAndNameChecks(string name,string type,string content) => Assert.Throws<RequestValidationException>(()=>CommunicationService.ValidateFile(name,type,System.Text.Encoding.UTF8.GetBytes(content)));
    [Fact] public void AllowedSignaturesAndSizeBoundaryAreChecked()
    {
        CommunicationService.ValidateFile("x.PDF","application/pdf","%PDF-1.7"u8.ToArray());
        CommunicationService.ValidateFile("x.jpeg","image/jpeg",[255,216,255]);CommunicationService.ValidateFile("x.png","image/png",[137,80,78,71,13,10,26,10]);
        var bytes=new byte[StoredDocument.MaxBytes];"%PDF-"u8.CopyTo(bytes);CommunicationService.ValidateFile("x.pdf","application/pdf",bytes);
        Assert.Throws<DocumentSizeException>(()=>CommunicationService.ValidateFile("x.pdf","application/pdf",new byte[StoredDocument.MaxBytes+1]));
        Assert.Throws<RequestValidationException>(()=>CommunicationService.ValidateFile("x.pdf","application/pdf",[]));
    }
    [Fact] public void ObjectIdentityIsServerGeneratedAndNeverContainsFilename()
    {
        var property=Guid.NewGuid();var document=new StoredDocument(property,Guid.NewGuid(),new string('A',64),null,ContentAudience.MANAGEMENT,"invoice.pdf","application/pdf",12,new string('B',64));
        Assert.Equal($"properties/{property:N}/documents/{document.Id:N}",document.ObjectKey);Assert.DoesNotContain("invoice",document.ObjectKey);
    }
}
