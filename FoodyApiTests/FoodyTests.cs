using NUnit.Framework;
using RestSharp;
using System.Net;
using System.Text.Json;

namespace FoodyApiTests
{
    public class FoodyTests
    {
        private RestClient client;
        private string token;
        private static string? lastFoodId;

        [OneTimeSetUp]
        public void Setup()
        {
            string baseUrl = Environment.GetEnvironmentVariable("BASE_URL")
                ?? "http://144.91.123.158:81/api";

            client = new RestClient(baseUrl);

            string uniqueUserName = "user" + DateTime.Now.Ticks;
            string password = "123456";

            var registerRequest = new RestRequest("/User/Create", Method.Post);
            registerRequest.AddJsonBody(new
            {
                userName = uniqueUserName,
                firstName = "Test",
                midName = "Test",
                lastName = "User",
                email = $"{uniqueUserName}@abv.bg",
                password,
                rePassword = password
            });

            client.Execute(registerRequest);

            var loginRequest = new RestRequest("/User/Authentication", Method.Post);
            loginRequest.AddJsonBody(new
            {
                userName = uniqueUserName,
                password
            });

            var loginResponse = client.Execute(loginRequest);

            var json = JsonSerializer.Deserialize<JsonElement>(loginResponse.Content!);
            token = json.GetProperty("accessToken").GetString()!;
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            client?.Dispose();
        }

        [Test, Order(1)]
        public void CreateFood_WithRequiredFields_ShouldCreateSuccessfully()
        {
            var request = new RestRequest("/Food/Create", Method.Post);
            request.AddHeader("Authorization", $"Bearer {token}");

            request.AddJsonBody(new
            {
                name = "Test Food",
                description = "Test Description",
                url = ""
            });

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content!);
            lastFoodId = json.GetProperty("foodId").GetString()!;

            Assert.That(lastFoodId, Is.Not.Null.And.Not.Empty);
        }

        [Test, Order(2)]
        public void EditFoodTitle_WithValidFoodId_ShouldEditSuccessfully()
        {
            var request = new RestRequest($"/Food/Edit/{lastFoodId}", Method.Patch);
            request.AddHeader("Authorization", $"Bearer {token}");

            var body = new[]
            {
                new
                {
                    path = "/name",
                    op = "replace",
                    value = "Edited Food"
                }
            };

            request.AddJsonBody(body);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content, Does.Contain("Successfully edited"));
        }

        [Test, Order(3)]
        public void GetAllFoods_ShouldReturnNonEmptyList()
        {
            var request = new RestRequest("/Food/All", Method.Get);
            request.AddHeader("Authorization", $"Bearer {token}");

            var response = client.Execute(request);

            var foods = JsonSerializer.Deserialize<List<JsonElement>>(response.Content!);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(foods, Is.Not.Null);
            Assert.That(foods!.Count, Is.GreaterThan(0));
        }

        [Test, Order(4)]
        public void DeleteFood_WithValidId_ShouldDeleteSuccessfully()
        {
            var request = new RestRequest($"/Food/Delete/{lastFoodId}", Method.Delete);
            request.AddHeader("Authorization", $"Bearer {token}");

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content, Does.Contain("Deleted successfully"));
        }

        [Test, Order(5)]
        public void CreateFood_WithoutRequiredFields_ShouldReturnBadRequest()
        {
            var request = new RestRequest("/Food/Create", Method.Post);
            request.AddHeader("Authorization", $"Bearer {token}");

            request.AddJsonBody(new { });

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test, Order(6)]
        public void EditFood_WithInvalidId_ShouldReturnNotFound()
        {
            var request = new RestRequest("/Food/Edit/invalid-id", Method.Patch);
            request.AddHeader("Authorization", $"Bearer {token}");

            var body = new[]
            {
                new
                {
                    path = "/name",
                    op = "replace",
                    value = "Test"
                }
            };

            request.AddJsonBody(body);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(response.Content, Does.Contain("No food revues"));
        }

        [Test, Order(7)]
        public void DeleteFood_WithInvalidId_ShouldReturnNotFound()
        {
            var request = new RestRequest("/Food/Delete/invalid-id", Method.Delete);
            request.AddHeader("Authorization", $"Bearer {token}");

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(response.Content, Does.Contain("No food revues"));
        }
    }
}