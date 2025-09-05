
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
// using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityIntelligenceMCP.Editor.Models;

namespace UnityIntelligenceMCP.Editor.Services
{
    public class UnityPackageService
    {
        public static List<UnityPackageData> GetInstalledPackages()
        {
            ListRequest request = Client.List();
            while (!request.IsCompleted)
            {
                continue;
            }
            if (request.Status == StatusCode.Success)
            {
                var packages = request.Result.Select(p => new UnityPackageData
                {
                    Name = p.name,
                    DisplayName = p.displayName,
                    Version = p.version
                }).ToList();
                return packages;
            }
            else
            {
                throw new Exception(request.Error.message);
            }
        }

        public static List<UnityPackageData> GetAvailablePackages()
        {
            var tcs = new TaskCompletionSource<ResourceResponse>();
            SearchRequest request = Client.SearchAll();
            while (!request.IsCompleted)
            {
                continue;
            }
            if (request.Status == StatusCode.Success)
            {
                ListRequest listRequest = Client.List();
                while (!listRequest.IsCompleted)
                {
                    continue;
                }
                List<PackageInfo> installed = new ();
                if (listRequest.Status == StatusCode.Success)
                {
                    installed = listRequest.Result.ToList();
                }
                var packages = request.Result.Select(p => new UnityPackageData
                {
                    Name = p.name,
                    DisplayName = p.displayName,
                    Version = p.version,
                    Installed = installed.Any(iP => iP.name == p.name)
                }).ToList();
                    return packages;
            }
            else
            {
                throw new Exception(request.Error.message);
            }
        }

        public static UnityPackageData GetPackageInfo(string packageName)
        {
            var tcs = new TaskCompletionSource<ResourceResponse>();
            // Check local packages first
            ListRequest listRequest = Client.List();
            while (!listRequest.IsCompleted)
            {
                continue;
            }
            List<PackageInfo> installed = new ();
            PackageInfo installedPackage = null;
            if (listRequest.Status == StatusCode.Success && listRequest.Result.Count() > 0)
            {
                installed = listRequest.Result.ToList();
                installedPackage = installed.Find(package => package.name == packageName);
            }
            // Check upstream for package to confirm versions (wrap in try in event of local packages in use)
            try
            {
                SearchRequest request = Client.Search(packageName);
                while (!request.IsCompleted)
                {
                    continue;
                }
                if (request.Status == StatusCode.Success)
                {
                    var result = request.Result;
                    if (result.Length > 0)
                    {
                        // Prioritize returning installed package data
                        var package = installedPackage ?? result.First();
                        return new UnityPackageData
                        {
                            Name = package.name,
                            DisplayName = package.displayName,
                            Version = package.version,
                            Installed = installedPackage != null,
                            Description = package.description
                        };
                    } throw new Exception("Empty package result received from Unity.");
                }
                else
                {
                    throw new Exception(request.Error.message);
                }
            }
            catch (Exception e)
            {
                if (installedPackage == null) throw new Exception(e.ToString());
                // custom package in use
                return new UnityPackageData
                {
                    Name = installedPackage.name,
                    DisplayName = installedPackage.displayName,
                    Version = installedPackage.version,
                    Installed = true,
                    Description = installedPackage.description
                };
            }
        }
    }
}