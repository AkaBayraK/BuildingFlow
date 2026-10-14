using Amazon.S3;
using BuildingFlow.Infrastructure;
namespace BuildingFlow.IntegrationTests;
internal static class StorageTestClient {
    public static AmazonS3Client Create() => S3FileStorage.CreateClient(
        Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_STORAGE_ENDPOINT") ?? "",
        Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_STORAGE_ACCESS_KEY") ?? "",
        Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_STORAGE_SECRET_KEY") ?? "");
}
