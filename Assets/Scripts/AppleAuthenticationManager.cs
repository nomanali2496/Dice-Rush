using AppleAuth;
using AppleAuth.Enums;
using AppleAuth.Extensions;
using AppleAuth.Interfaces;
using AppleAuth.Native;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;
using System.Security.Cryptography;
using System;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

    public class AppleAuthenticationManager : MonoBehaviour
    {
        public static AppleAuthenticationManager Instance;
    public static Action<bool, string,string> AfterAuthenticationResp;

    private IAppleAuthManager appleAuthManager;

        private void Awake()
        {
            if (Instance == null) {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            } else {
                Destroy(gameObject);
            }
        }
        void Start()
        {
            //gameObject.SetActive(AppleAuthManager.IsCurrentPlatformSupported);
            // If the current platform is supported
            if (AppleAuthManager.IsCurrentPlatformSupported && appleAuthManager==null)
            {
                // Creates a default JSON deserializer, to transform JSON Native responses to C# instances
                var deserializer = new PayloadDeserializer();
                // Creates an Apple Authentication manager with the deserializer
                this.appleAuthManager = new AppleAuthManager(deserializer);
            }
        }

        void Update()
        {
            // Updates the AppleAuthManager instance to execute
            // pending callbacks inside Unity's execution loop
            if(AppleAuthManager.IsCurrentPlatformSupported && appleAuthManager != null)
                appleAuthManager.Update();
        }

        public void PerformAppleLogin()
        {
            if (!AppleAuthManager.IsCurrentPlatformSupported)
            {
                Debug.LogError("Not on supported platform:");
            AfterAuthenticationResp?.Invoke(true, "eyJraWQiOiJyQlJmVm1xc2puIiwiYWxnIjoiUlMyNTYifQ.eyJpc3MiOiJodHRwczovL2FwcGxlaWQuYXBwbGUuY29tIiwiYXVkIjoiY29tLmRvbWFpbi55aXlpIiwiZXhwIjoxNzQxMjAzODkyLCJpYXQiOjE3NDExMTc0OTIsInN1YiI6IjAwMDg5Ny41ZmM3N2I3ZmQxMjk0YWE1YWVlZTNlNmVlYjQ0MTQ1My4xOTQ0Iiwibm9uY2UiOiI2YjEwNDU5YjIzYWQ3MjcwZjE4YzE3NGVjNjQzMGFjYmQ4ZGE2MWQwNmY1YWFkOTViMDM1ODU2NGY5OGNhZDA2IiwiY19oYXNoIjoiVFBlQk5NdzIzSy10WWI5OEY5WFdvQSIsImVtYWlsIjoiemI3cHFnc3ZmYkBwcml2YXRlcmVsYXkuYXBwbGVpZC5jb20iLCJlbWFpbF92ZXJpZmllZCI6dHJ1ZSwiYXV0aF90aW1lIjoxNzQxMTE3NDkyLCJub25jZV9zdXBwb3J0ZWQiOnRydWUsInJlYWxfdXNlcl9zdGF0dXMiOjB9.W7cAak6mnErkdp5-2aeN8KxSL2uQf3JzYu1ro3kMdsAvgElVEUnchekjF5a1YA1URoan5v7osc-XQ-OTGuIxpvLC2sPqsBO50IFs-eVbQCDfqgv8BK5OEQlWLuQRE-b-GadWbNYm3l3Eb2WW9WIs9IdUy18APpbz1EDlpk7K1M0ecQvuAAqImpFtfOgMHVHSdBglp60sUyRTR_gh8G8jH-87qegp63IVk-kUgGI6bZzUk0QFtodaGIWDFH063-AmZKhxdXXaijIFJD8sU2QqAr22RynbYXhduSGsGz9I3eyW-N-G3JMZn1OP1PKGMQ60huW5m1JMEQ2oVCWWuaE1Tg", "Nasir");
            return;
            }

            var rawNonce = GenerateRandomString(32);
            var nonce = GenerateSHA256NonceFromRawNonce(rawNonce);

            var loginArgs = new AppleAuthLoginArgs(
                LoginOptions.IncludeEmail | LoginOptions.IncludeFullName,
                nonce);

            this.appleAuthManager.LoginWithAppleId(
                loginArgs,
                credential =>
                {
                    var appleIdCredential = credential as IAppleIDCredential;
                    if (appleIdCredential != null)
                    {
                        string firstName = PlayerPrefs.GetString("AppleFirstName", "");
                        string lastName = PlayerPrefs.GetString("AppleLastName", "");

                        // Check if the full name is provided (first-time login)
                        if (appleIdCredential.FullName != null)
                        {
                            firstName = appleIdCredential.FullName?.GivenName;
                            lastName = appleIdCredential.FullName?.FamilyName;

                            // Save the first and last names for future use
                            if (!string.IsNullOrEmpty(firstName) && !string.IsNullOrEmpty(lastName))
                            {
                                PlayerPrefs.SetString("AppleFirstName", firstName);
                                PlayerPrefs.SetString("AppleLastName", lastName);
                                PlayerPrefs.Save();
                            }
                        }
                       

                        string displayName = $"{firstName} {lastName}";
                        Debug.Log("DisplayName: " + displayName);

                        var identityToken = Encoding.UTF8.GetString(appleIdCredential.IdentityToken);
                        var authorizationCode = Encoding.UTF8.GetString(appleIdCredential.AuthorizationCode);
                        var userId = appleIdCredential.User;
                        var email = appleIdCredential.Email; // Get email if available

                Debug.Log($"IdentityToken: {identityToken}");
                        Debug.Log($"AuthorizationCode: {authorizationCode}");
                        Debug.Log($"Email: {email}");
                        Debug.Log($"FirstName: {firstName}");
                        Debug.Log($"LastName: {lastName}");
                        Debug.Log($"UserId: {userId}");

                        AfterAuthenticationResp?.Invoke(true, identityToken, displayName);

                        //var appleData = new SocialSignUpDataApple
                        //{
                        //    firstName = firstName,
                        //    lastName = lastName,
                        //    accessToken = identityToken,
                        //    platform = "apple"
                        //};

                    }
                    else
                    {
                        AfterAuthenticationResp?.Invoke(false, "Credential is null","");
                        Debug.Log("Credential is null");
                    }
                },
                error =>
                {
                    AfterAuthenticationResp?.Invoke(false, "Login cancelled by user!","");
                    Debug.Log("Login cancelled by user!");
                });
        }
        public DateTime? GetExpirationFromAppleToken(string identityToken)
        {
            if (string.IsNullOrEmpty(identityToken))
            {
                Debug.LogError("Identity token is null or empty.");
                return null;
            }

            try
            {
                // Split the JWT token into header, payload, and signature
                var parts = identityToken.Split('.');
                if (parts.Length < 2)
                {
                    Debug.LogError("Invalid JWT token structure.");
                    return null;
                }

                // Decode the payload (Base64Url decoding)
                var payload = parts[1];
                payload = payload.Replace('-', '+').Replace('_', '/');
                switch (payload.Length % 4)
                {
                    case 2: payload += "=="; break;
                    case 3: payload += "="; break;
                }

                var decodedBytes = Convert.FromBase64String(payload);
                var decodedJson = Encoding.UTF8.GetString(decodedBytes);

                // Parse the JSON payload
                var jsonPayload = JObject.Parse(decodedJson);
                var expTimestamp = jsonPayload["exp"]?.Value<long>();

                if (expTimestamp.HasValue)
                {
                    // Convert Unix timestamp to DateTime
                    return DateTimeOffset.FromUnixTimeSeconds(expTimestamp.Value).UtcDateTime;
                }
                else
                {
                    Debug.LogError("Expiration (exp) claim not found in token.");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error decoding token: {ex.Message}");
                return null;
            }
        }

        private string GenerateRandomString(int length)
        {
            if (length <= 0)
            {
                throw new System.Exception("Expected nonce to have positive length");
            }

            const string charset = "0123456789ABCDEFGHIJKLMNOPQRSTUVXYZabcdefghijklmnopqrstuvwxyz-._";
            var cryptographicallySecureRandomNumberGenerator = new RNGCryptoServiceProvider();
            var result = string.Empty;
            var remainingLength = length;

            var randomNumberHolder = new byte[1];
            while (remainingLength > 0)
            {
                var randomNumbers = new List<int>(16);
                for (var randomNumberCount = 0; randomNumberCount < 16; randomNumberCount++)
                {
                    cryptographicallySecureRandomNumberGenerator.GetBytes(randomNumberHolder);
                    randomNumbers.Add(randomNumberHolder[0]);
                }

                for (var randomNumberIndex = 0; randomNumberIndex < randomNumbers.Count; randomNumberIndex++)
                {
                    if (remainingLength == 0)
                    {
                        break;
                    }

                    var randomNumber = randomNumbers[randomNumberIndex];
                    if (randomNumber < charset.Length)
                    {
                        result += charset[randomNumber];
                        remainingLength--;
                    }
                }
            }

            return result;
        }

        private string GenerateSHA256NonceFromRawNonce(string rawNonce)
        {
            var sha = new SHA256Managed();
            var utf8RawNonce = Encoding.UTF8.GetBytes(rawNonce);
            var hash = sha.ComputeHash(utf8RawNonce);

            var result = string.Empty;
            for (var i = 0; i < hash.Length; i++)
            {
                result += hash[i].ToString("x2");
            }

            return result;
        }
    }
