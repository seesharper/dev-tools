#!/usr/bin/env dotnet-script
#load "git.csx"
#load "graphql.csx"

using System;
using System.Net.Http;

string query = @"
query ($owner: String!, $name : String!){
 	repository(owner : $owner,name:$name)
  {
  	issues(last:10,states:OPEN, orderBy: {field:CREATED_AT direction: ASC} ){
      nodes{
        number,
        title,
        createdAt,
        url
      }
    }
  }
}
";

var apiKey = System.Environment.GetEnvironmentVariable("ISSUES_API_KEY");

if (string.IsNullOrWhiteSpace(apiKey))
{
    Error.WriteLine("No API key found. export ISSUES_API_KEY='YOUR_API_KEY'");
    return 0xbad;
}

var httpClient = new HttpClient { BaseAddress = new Uri("https://api.github.com/graphql") };
httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
httpClient.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue(new System.Net.Http.Headers.ProductHeaderValue("Issues")));

var repo = Git.GetRepositoryInfo();

var result = await httpClient.ExecuteAsync(query, new { owner = repo.Owner, name = repo.ProjectName });

var issues = result.Get<Issue[]>("repository.issues.nodes");
foreach (var issue in issues.OrderByDescending(i => i.CreatedAt))
{
    WriteLine($"#{issue.Number} {issue.Title} ({issue.Url}) ({issue.CreatedAt.ToShortDateString()})");
}

public class Issue
{
    public int Number { get; set; }

    public string Title { get; set; }

    public DateTime CreatedAt { get; set; }

    public string Url { get; set; }
}