using K.Basis.Domain.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace LightBlog.Infrastruct.Entities
{
    public class Post : Entity<int>
    {
        public Post(string title, string entryName, string content, string excerpt, bool isPublished)
        {
            Title = title ?? throw new ArgumentNullException(nameof(title));
            EntryName = entryName;
            Content = content ?? throw new ArgumentNullException(nameof(content));
            Excerpt = excerpt;
            PostViews = 0;
            Published = DateTime.UtcNow;
            LastModified = DateTime.UtcNow;
            IsPublished = isPublished;
        }

        public void SetAuthor(int authorId, string authorName)
        {
            AuthorId = authorId;
            AuthorName = authorName ?? throw new ArgumentNullException(nameof(authorName));
        }

        public void Edit(string title, string entryName, string content, string excerpt)
        {
            Title = title ?? throw new ArgumentNullException(nameof(title));
            EntryName = entryName;
            Content = content ?? throw new ArgumentNullException(nameof(content));
            Excerpt = excerpt;
            LastModified = DateTime.UtcNow;
        }

        public void AddComment(string content, int commentatorId, string commentatorName)
        {
            Comments.Add(new Comment(Id,content,commentatorId, commentatorName));
            CommentCount++;
        }

        [Required]
        public int AuthorId { get; private set; }

        [Required]
        [StringLength(60)]
        public string AuthorName { get; private set; }

        /// <summary>
        /// 博客标题
        /// </summary>
        [Required]
        [StringLength(160)]
        public string Title { get; private set; }

        /// <summary>
        /// 博客友好地址
        /// </summary>
        [RegularExpression("^[a-z0-9-]+$", ErrorMessage = "Slug format not valid.")]
        [StringLength(160)]
        public string EntryName { get; private set; }

        /// <summary>
        /// 博客内容
        /// </summary>
        [Required]
        public string Content { get; private set; }

        /// <summary>
        /// 摘要
        /// </summary>
        [StringLength(2000)]
        public string Excerpt { get; private set; }

        /// <summary>
        /// Tag标签
        /// </summary>
        [StringLength(500)]
        public string Tags { get; private set; }

        /// <summary>
        /// 阅读量
        /// </summary>
        public int PostViews { get; private set; }

        /// <summary>
        /// 评论
        /// </summary>
        public virtual List<Comment> Comments { get; private set; }

        public int CommentCount { get; private set; } = 0;

        [Required]
        public DateTime Published { get; private set; }

        public DateTime LastModified { get; private set; }

        public bool IsPublished { get; private set; } = true;
    }

    public class Comment : Entity<int>
    {
        public Comment(int postId, string content, int commentatorId, string commentatorName)
        {
            PostId = postId;
            Content = content ?? throw new ArgumentNullException(nameof(content));
            CommentatorId = commentatorId;
            CommentatorName = commentatorName ?? throw new ArgumentNullException(nameof(commentatorName));
            CreatedTime = DateTime.UtcNow;
        }

        [Required]
        public int CommentatorId { get; private set; }

        [Required]
        [StringLength(60)]
        public string CommentatorName { get; private set; }

        [Required]
        public int PostId { get; private set; }
        [Required]
        public virtual Post Post { get; private set; }

        [Required]
        [StringLength(1000)]
        public string Content { get; private set; }

        [Required]
        public DateTime CreatedTime { get; set; }
    }
}
