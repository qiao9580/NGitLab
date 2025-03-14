﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NGitLab.Mock.Internals;
using NGitLab.Models;

namespace NGitLab.Mock.Clients;

internal sealed class ReleaseClient : ClientBase, IReleaseClient
{
    private readonly long _projectId;

    public ReleaseClient(ClientContext context, ProjectId projectId)
        : base(context)
    {
        _projectId = Server.AllProjects.FindProject(projectId.ValueAsString()).Id;
    }

    public IEnumerable<Models.ReleaseInfo> All
    {
        get
        {
            using (Context.BeginOperationScope())
            {
                var project = GetProject(_projectId, ProjectPermission.View);
                return project.Releases.Select(r => r.ToReleaseClient());
            }
        }
    }

    public Models.ReleaseInfo this[string tagName]
    {
        get
        {
            using (Context.BeginOperationScope())
            {
                var project = GetProject(_projectId, ProjectPermission.View);
                var release = project.Releases.FirstOrDefault(r => r.TagName.Equals(tagName, StringComparison.Ordinal));

                return release.ToReleaseClient();
            }
        }
    }

    public Models.ReleaseInfo Create(ReleaseCreate data)
    {
        using (Context.BeginOperationScope())
        {
            var project = GetProject(_projectId, ProjectPermission.Contribute);
            var release = project.Releases.Add(data.TagName, data.Name, data.Ref, data.Description, Context.User);
            return release.ToReleaseClient();
        }
    }

    public Models.ReleaseInfo Update(ReleaseUpdate data)
    {
        using (Context.BeginOperationScope())
        {
            var project = GetProject(_projectId, ProjectPermission.Contribute);
            var release = project.Releases.GetByTagName(data.TagName);
            if (release == null)
            {
                throw GitLabException.NotFound();
            }

            if (data.Name != null)
            {
                release.Name = data.Name;
            }

            if (data.Description != null)
            {
                release.Description = data.Description;
            }

            if (data.ReleasedAt.HasValue)
            {
                release.ReleasedAt = data.ReleasedAt.Value;
            }

            return release.ToReleaseClient();
        }
    }

    public void Delete(string tagName)
    {
        using (Context.BeginOperationScope())
        {
            var project = GetProject(_projectId, ProjectPermission.Contribute);
            var release = project.Releases.FirstOrDefault(r => r.TagName.Equals(tagName, StringComparison.Ordinal));
            if (release == null)
                throw GitLabException.NotFound();

            project.Releases.Remove(release);
        }
    }

    public IReleaseLinkClient ReleaseLinks(string tagName)
    {
        throw new NotImplementedException();
    }

    [SuppressMessage("Design", "MA0042:Do not use blocking calls in an async method", Justification = "Would be an infinite recursion")]
    public async Task<Models.ReleaseInfo> CreateAsync(ReleaseCreate data, CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        return Create(data);
    }

    public GitLabCollectionResponse<Models.ReleaseInfo> GetAsync(ReleaseQuery query = null)
    {
        using (Context.BeginOperationScope())
        {
            var project = GetProject(_projectId, ProjectPermission.View);
            var result = project.Releases.AsEnumerable();
            if (query != null)
            {
                var orderBy = !string.IsNullOrEmpty(query.OrderBy) && string.Equals(query.OrderBy, "created_at", StringComparison.Ordinal)
                    ? new Func<ReleaseInfo, DateTime>(r => r.CreatedAt)
                    : new Func<ReleaseInfo, DateTime>(r => r.ReleasedAt);

                var sortAsc = !string.IsNullOrEmpty(query.Sort) && string.Equals(query.Sort, "asc", StringComparison.Ordinal);
                result = sortAsc ? result.OrderBy(orderBy) : result.OrderByDescending(orderBy);

                if (query.Page.HasValue)
                {
                    var perPage = query.PerPage ?? 20;
                    var page = Math.Max(0, query.Page.Value - 1);
                    result = result.Skip(perPage * page);
                }

                if (query.IncludeHtmlDescription == true)
                    throw new NotImplementedException();
            }

            return GitLabCollectionResponse.Create(result.Select(r => r.ToReleaseClient()).ToArray());
        }
    }
}
