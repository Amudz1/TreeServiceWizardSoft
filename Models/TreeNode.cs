using System.ComponentModel.DataAnnotations;

namespace TreeService.Models;

public class TreeNode
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public int? ParentId { get; set; }

    public TreeNode? Parent { get; set; }

    public ICollection<TreeNode> Children { get; set; } = new List<TreeNode>();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
