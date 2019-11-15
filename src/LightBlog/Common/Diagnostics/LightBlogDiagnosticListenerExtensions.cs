using LightBlog.Infrastruct.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace LightBlog.Common.Diagnostics
{
    public static class LightBlogDiagnosticListenerExtensions
    {
        public const string DiagnosticListenerName = "LightBlogDiagnosticListener";

        private const string CapPrefix = "LightBlog.HomeCache.";

        public const string AfterPostAddComment = CapPrefix + nameof(WriteAddPostCommentAfter);

        public const string AfterDeletePost = CapPrefix + nameof(WriteDeletePostAfter);

        public const string AfterAddOrUpdatePost = CapPrefix + nameof(WriteAddOrUpdatePostAfter);

        public static void WriteAddPostCommentAfter(this DiagnosticListener @this, int postId)
        {
            if (@this.IsEnabled(AfterPostAddComment))
            {
                @this.Write(AfterPostAddComment, postId);
            }
        }

        public static void WriteDeletePostAfter(this DiagnosticListener @this, int postId)
        {
            if (@this.IsEnabled(AfterDeletePost))
            {
                @this.Write(AfterDeletePost, postId);
            }
        }

        public static void WriteAddOrUpdatePostAfter(this DiagnosticListener @this, Post post)
        {
            if (@this.IsEnabled(AfterAddOrUpdatePost))
            {
                @this.Write(AfterAddOrUpdatePost, post);
            }
        }
    }
}
