#if UNITY_IOS || UNITY_TVOS

using System;
using System.Reflection;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace AppleAuth.Editor
{
    public static class ProjectCapabilityManagerExtension
    {
        private const string EntitlementsArrayKey = "com.apple.developer.applesignin";
        private const string DefaultAccessLevel = "Default";
        private const string AuthenticationServicesFramework = "AuthenticationServices.framework";
        private const BindingFlags NonPublicInstanceBinding = BindingFlags.NonPublic | BindingFlags.Instance;

        /// <summary>
        /// Extension method for ProjectCapabilityManager to add the Sign In With Apple capability in compatibility mode.
        /// In particular, adds the AuthenticationServices.framework as an Optional framework, preventing crashes in
        /// iOS versions previous to 13.0
        /// </summary>
        /// <param name="manager">The manager for the main target to use when adding the Sign In With Apple capability.</param>
        /// <param name="unityFrameworkTargetGuid">The GUID for the UnityFramework target. If null, it will use the main target GUID.</param>
        public static void AddSignInWithAppleWithCompatibility(this ProjectCapabilityManager manager, string unityFrameworkTargetGuid = null)
        {
            try
            {
                var managerType = typeof(ProjectCapabilityManager);
                var capabilityTypeType = typeof(PBXCapabilityType);

                // Get private fields using reflection
                var projectField = managerType.GetField("project", NonPublicInstanceBinding);
                var targetGuidField = managerType.GetField("m_TargetGuid", NonPublicInstanceBinding);
                var entitlementFilePathField = managerType.GetField("m_EntitlementFilePath", NonPublicInstanceBinding);
                var getOrCreateEntitlementDocMethod = managerType.GetMethod("GetOrCreateEntitlementDoc", NonPublicInstanceBinding);
                var constructorInfo = capabilityTypeType.GetConstructor(
                    NonPublicInstanceBinding,
                    null,
                    new[] { typeof(string), typeof(bool), typeof(string), typeof(bool) },
                    null);

                // Check if required fields and methods are found
                if (projectField == null || targetGuidField == null || entitlementFilePathField == null ||
                    getOrCreateEntitlementDocMethod == null || constructorInfo == null)
                {
                    throw new Exception("Can't Add Sign In With Apple programatically in this Unity version");
                }

                // Get entitlement file path and entitlement document
                var entitlementFilePath = entitlementFilePathField.GetValue(manager) as string;
                var entitlementDoc = getOrCreateEntitlementDocMethod.Invoke(manager, new object[] { }) as PlistDocument;

                if (entitlementDoc != null)
                {
                    // Add Sign In With Apple entitlement
                    var plistArray = new PlistElementArray();
                    plistArray.AddString(DefaultAccessLevel);
                    entitlementDoc.root[EntitlementsArrayKey] = plistArray;

                    Debug.Log($"Successfully added entitlement for Sign In With Apple to {entitlementFilePath}");
                }
                else
                {
                    Debug.LogError("Failed to create entitlement document for Sign In With Apple");
                    return;
                }

                var project = projectField.GetValue(manager) as PBXProject;
                if (project != null)
                {
                    var mainTargetGuid = targetGuidField.GetValue(manager) as string;
                    var capabilityType = constructorInfo.Invoke(new object[] { "com.apple.developer.applesignin.custom", true, string.Empty, true }) as PBXCapabilityType;

                    var targetGuidToAddFramework = unityFrameworkTargetGuid ?? mainTargetGuid;

                    // Add the AuthenticationServices framework as optional
                    project.AddFrameworkToProject(targetGuidToAddFramework, AuthenticationServicesFramework, true);
                    Debug.Log($"Added {AuthenticationServicesFramework} as an optional framework");

                    // Add the capability for Sign In With Apple
                    project.AddCapability(mainTargetGuid, capabilityType, entitlementFilePath, false);
                    Debug.Log("Added Sign In With Apple capability");
                }
                else
                {
                    Debug.LogError("Failed to get PBXProject instance from ProjectCapabilityManager");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error while adding Sign In With Apple capability: {e.Message}\n{e.StackTrace}");
            }
        }
    }
}

#endif
