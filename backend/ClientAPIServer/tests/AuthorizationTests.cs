using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MockServerAPITests
{
    [TestClass]
    public class AuthorizationTests : MockApiTestsBase
    {
        [TestMethod]
        public async Task CheckSession_WithValidSessionID_ReturnsSuccess()
        {
            // Arrange - base class already sets up Authorization header with "SessionID <guid>"
            
            // Act
            var response = await _client.GetAsync("/api/v3/CheckSession");
            
            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var json = await response.Content.ReadAsStringAsync();
            Assert.IsTrue(json.Contains("\"status\"") && json.Contains("\"active\""));
            Assert.IsTrue(json.Contains("\"sessionId\"") && json.Contains(_sessionId));
        }

        [TestMethod]
        public async Task CheckSession_WithRandomString_ReturnsSuccess()
        {
            // Arrange - Test with non-GUID session ID
            var randomSessionId = "test-session-" + Guid.NewGuid().ToString().Substring(0, 8);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("SessionID", randomSessionId);
            
            // Act
            var response = await _client.GetAsync("/api/v3/CheckSession");
            
            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var json = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Response JSON: {json}");
            Assert.IsTrue(json.Contains("\"status\"") && json.Contains("\"active\""), $"Expected status:active in response: {json}");
            Assert.IsTrue(json.Contains("\"sessionId\"") && json.Contains(randomSessionId), $"Expected sessionId:{randomSessionId} in response: {json}");
        }

        [TestMethod]
        public async Task CheckSession_WithoutAuthorization_ReturnsUnauthorized()
        {
            // Arrange - Remove authorization header
            _client.DefaultRequestHeaders.Authorization = null;
            
            // Act
            var response = await _client.GetAsync("/api/v3/CheckSession");
            
            // Assert
            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
            var json = await response.Content.ReadAsStringAsync();
            Assert.IsTrue(json.Contains("Authorization header must contain"));
        }

        [TestMethod]
        public async Task CheckSession_WithWrongScheme_ReturnsUnauthorized()
        {
            // Arrange - Use Bearer instead of SessionID
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "some-token");
            
            // Act
            var response = await _client.GetAsync("/api/v3/CheckSession");
            
            // Assert
            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [TestMethod]
        public async Task CheckSession_WithEmptySessionID_ReturnsUnauthorized()
        {
            // Arrange - SessionID with empty value
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("SessionID", "");
            
            // Act
            var response = await _client.GetAsync("/api/v3/CheckSession");
            
            // Assert
            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [TestMethod]
        public async Task Authorization_FormatTest_CorrectFormat()
        {
            // Test that the format is exactly "SessionID <value>" as per spec
            var testSessionId = "my-test-session-123";
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/v3/CheckSession");
            request.Headers.Authorization = new AuthenticationHeaderValue("SessionID", testSessionId);
            
            // The Authorization header should be: "SessionID my-test-session-123"
            Assert.AreEqual("SessionID", request.Headers.Authorization.Scheme);
            Assert.AreEqual(testSessionId, request.Headers.Authorization.Parameter);
            
            var response = await _client.SendAsync(request);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }
    }
}