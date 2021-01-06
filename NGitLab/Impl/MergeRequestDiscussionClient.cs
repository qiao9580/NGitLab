﻿using System.Collections.Generic;
using System.Globalization;
using NGitLab.Models;

namespace NGitLab.Impl
{
    public class MergeRequestDiscussionClient : IMergeRequestDiscussionClient
    {
        private readonly API _api;
        private readonly string _discussionsPath;

        public MergeRequestDiscussionClient(API api, string projectPath, int mergeRequestIid)
        {
            _api = api;
            _discussionsPath = projectPath + "/merge_requests/" + mergeRequestIid.ToString(CultureInfo.InvariantCulture) + "/discussions";
        }

        public IEnumerable<MergeRequestDiscussion> All => _api.Get().GetAll<MergeRequestDiscussion>(_discussionsPath);

        public MergeRequestDiscussion Add(MergeRequestDiscussionCreate comment) => _api.Post().With(comment).To<MergeRequestDiscussion>(_discussionsPath);

        public MergeRequestDiscussion Resolve(MergeRequestDiscussionResolve resolve) => _api.Put().With(resolve).To<MergeRequestDiscussion>(_discussionsPath + "/" + resolve.Id);

        public void Delete(string discussionId, long commentId) => _api.Delete().Execute(_discussionsPath + "/" + discussionId + "/" + "notes/" + commentId.ToString(CultureInfo.InvariantCulture));
    }
}
