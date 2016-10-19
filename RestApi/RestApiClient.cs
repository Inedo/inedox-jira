﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;

namespace Inedo.BuildMasterExtensions.Jira.RestApi
{
    internal sealed class RestApiClient
    {
        private string host;
        private string apiBaseUrl;

        public RestApiClient(string host)
        {
            this.host = host.TrimEnd('/');
            this.apiBaseUrl = this.host + "/rest/api/2/";
        }

        public string UserName { get; set; }
        public string Password { get; set; }

        public IEnumerable<Permission> GetPermissions()
        {
            var results = (Dictionary<string, object>)this.Invoke("GET", "mypermissions");
            var permissions = (Dictionary<string, object>)results["permissions"];
            foreach (var permission in permissions)
            {
                yield return new Permission((Dictionary<string, object>)permission.Value);
            }
        }

        public void AddComment(string issueKey, string comment)
        {
            this.Invoke("POST", $"issue/{issueKey}/comment", data: new { body = comment });
        }

        public IEnumerable<JiraProject> GetProjects()
        {
            var results = (IEnumerable<object>)this.Invoke("GET", "project");
            foreach (var result in results)
            {
                yield return new JiraProject((Dictionary<string, object>)result);
            }
        }

        public IEnumerable<ProjectVersion> GetVersions(string projectKey)
        {
            var results = (IEnumerable<object>)this.Invoke(
                "GET", 
                $"project/{projectKey}/versions"
            );

            return results.Select(v => ProjectVersion.Parse((Dictionary<string, object>)v)).Where(v => v != null);
        }

        public void TransitionIssue(string issueKey, string transitionId)
        {
            this.Invoke(
                "POST", 
                $"issue/{issueKey}/transitions", 
                data: new
                {
                    transition = new { id = transitionId }
                }
            );
        }

        public IEnumerable<Transition> GetTransitions(string issueKey)
        {
            var results = (Dictionary<string, object>)this.Invoke(
                "GET",
                $"issue/{issueKey}/transitions"
            );

            var transitions = (IEnumerable<object>)results["transitions"];
            foreach (var transition in transitions)
            {
                yield return new Transition((Dictionary<string, object>)transition);
            }
        }

        public void ReleaseVersion(string projectKey, string versionId)
        {
            this.Invoke(
                "PUT",
                $"version/{versionId}",
                data: new
                {
                    released = true,
                    releaseDate = DateTime.Now
                }
            );
        }

        public void CreateVersion(string projectKey, string versionNumber)
        {
            this.Invoke(
                "POST",
                "version",
                data: new
                {
                    name = versionNumber,
                    project = projectKey
                }
            );
        }

        public Issue GetIssue(string issueKey)
        {
            var result = (Dictionary<string, object>)this.Invoke(
                "GET",
                $"issue/{issueKey}"
            );

            return new Issue(result, this.host);
        }

        public IEnumerable<Issue> GetIssues(string projectKey, string versionName)
        {
            var result = (Dictionary<string, object>)this.Invoke(
                "GET",
                "search",
                new QueryString { Jql = $"project='{projectKey}' and fixVersion='{versionName}'" }
            );

            var issues = (IEnumerable<object>)result["issues"];
            
            foreach (var issue in issues)
            {
                yield return new Issue((Dictionary<string, object>)issue, this.host);
            }
        }

        private object Invoke(string method, string relativeUrl, QueryString query = null, object data = null)
        {
            string url = this.apiBaseUrl + relativeUrl + query?.ToString();

            var request = (HttpWebRequest)HttpWebRequest.Create(url);
            request.UserAgent = "BuildMasterJiraExtension/" + typeof(RestApiClient).Assembly.GetName().Version.ToString();
            request.ContentType = "application/json";
            request.Method = method;
            if (data != null)
            {
                using (var requestStream = request.GetRequestStream())
                using (var writer = new StreamWriter(requestStream, InedoLib.UTF8Encoding))
                {
                    InedoLib.Util.JavaScript.WriteJson(writer, data);
                }
            }

            if (!string.IsNullOrEmpty(this.UserName))
            {                
                request.Headers[HttpRequestHeader.Authorization] = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(this.UserName + ":" + this.Password));                
            }

            try
            {
                using (var response = request.GetResponse())
                using (var responseStream = response.GetResponseStream())
                using (var reader = new StreamReader(responseStream))
                {
                    var js = new JavaScriptSerializer();
                    return js.DeserializeObject(reader.ReadToEnd());
                }
            }
            catch (WebException ex)
            {
                if (ex.Response == null)
                    throw;

                using (var responseStream = ex.Response.GetResponseStream())
                {
                    string message;
                    try
                    {
                        var js = new JavaScriptSerializer();
                        var result = (Dictionary<string, object>)js.DeserializeObject(new StreamReader(responseStream).ReadToEnd());
                        var messages = (IEnumerable<object>)result["errorMessages"];
                        var errors = (Dictionary<string, object>)result["errors"];
                        message = "JIRA API response error: " + string.Join("; ", messages.Concat(errors.Select(e => $"{e.Value} ({e.Key})")));
                    }
                    catch
                    {
                        throw ex;
                    }

                    throw new Exception(message, ex);
                }
            }
        }
    }
}