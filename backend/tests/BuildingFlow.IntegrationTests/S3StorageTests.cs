using Amazon.S3;
using Amazon.S3.Model;
using BuildingFlow.Infrastructure;
using Xunit;
namespace BuildingFlow.IntegrationTests;
public sealed class S3StorageTests(SiteApiTests.SiteApiFactory factory) : IClassFixture<SiteApiTests.SiteApiFactory> {
    [StorageFact] public async Task SeaweedGatewaySupportsSignedPrivateRoundTrip() {
        using var client=StorageTestClient.Create();
        var stage="create";
        try {
            await client.PutBucketAsync(new PutBucketRequest {BucketName=factory.StorageBucket});
            stage="policy";
            try { var p=await client.GetBucketPolicyAsync(new GetBucketPolicyRequest {BucketName=factory.StorageBucket}); Assert.True(S3FileStorage.IsPrivatePolicyResponse(p)); }
            catch (AmazonS3Exception e) when (e.ErrorCode=="NoSuchBucketPolicy") { }
            stage="upload";
            using var content=new MemoryStream("S3 fixture"u8.ToArray());
            await client.PutObjectAsync(new PutObjectRequest {BucketName=factory.StorageBucket,Key="fixture.bin",InputStream=content,AutoCloseStream=false,UseChunkEncoding=false,ContentType="application/octet-stream"});
            stage="metadata";
            var meta=await client.GetObjectMetadataAsync(new GetObjectMetadataRequest {BucketName=factory.StorageBucket,Key="fixture.bin"});
            Assert.Equal(10,meta.ContentLength);
            stage="download";
            using var result=await client.GetObjectAsync(new GetObjectRequest {BucketName=factory.StorageBucket,Key="fixture.bin"});
            using var body=new MemoryStream();await result.ResponseStream.CopyToAsync(body);
            Assert.Equal("S3 fixture"u8.ToArray(),body.ToArray());
        } catch (AmazonS3Exception e) { throw new InvalidOperationException("S3 contract stage="+stage+" status="+(int)e.StatusCode+" code="+e.ErrorCode); }
        catch (Exception e) when (e is not Xunit.Sdk.XunitException) {throw new InvalidOperationException("S3 contract stage="+stage+" type="+e.GetType().Name);}
    }
}

public sealed class S3PolicyTests {
    [Theory]
    [InlineData(200,"",true)]
    [InlineData(200,"{\"Version\":\"2012-10-17\",\"Statement\":[{\"Effect\":\"Allow\"}]}",false)]
    [InlineData(404,"<Error><Code>NoSuchBucketPolicy</Code></Error>",true)]
    [InlineData(403,"<Error><Code>NoSuchBucketPolicy</Code></Error>",false)]
    [InlineData(404,"<Error><Code>AccessDenied</Code></Error>",false)]
    [InlineData(404,"<!DOCTYPE Error [<!ENTITY x SYSTEM 'file:///etc/passwd'>]><Error><Code>&x;</Code></Error>",false)]
    public void OnlyConfirmedMissingPolicyIsAccepted(int status,string body,bool expected) {
        Assert.Equal(expected,S3FileStorage.IsPrivatePolicyResponse(new GetBucketPolicyResponse {HttpStatusCode=(System.Net.HttpStatusCode)status,Policy=body}));
    }
}
