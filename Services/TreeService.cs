using Microsoft.EntityFrameworkCore;
using TreeService.Data;
using TreeService.DTOs;
using TreeService.Models;

namespace TreeService.Services;

public interface ITreeService
{
    Task<NodeDto> CreateNodeAsync(CreateNodeDto dto);
    Task<NodeDto?> GetNodeAsync(int id);
    Task<IEnumerable<NodeDto>> GetAllNodesAsync();
    Task<TreeNodeDto?> GetTreeAsync(int? rootId = null);
    Task<NodeDto?> UpdateNodeAsync(int id, UpdateNodeDto dto);
    Task<bool> DeleteNodeAsync(int id);
    Task<string> ExportTreeAsync(int? rootId = null);
}

public class TreeService : ITreeService
{
    private readonly ApplicationDbContext _context;

    public TreeService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<NodeDto> CreateNodeAsync(CreateNodeDto dto)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            if (dto.ParentId.HasValue)
            {
                var parentExists = await _context.TreeNodes
                    .AnyAsync(n => n.Id == dto.ParentId.Value);
                
                if (!parentExists)
                {
                    throw new InvalidOperationException("Parent node does not exist");
                }
            }

            var node = new TreeNode
            {
                Name = dto.Name,
                Description = dto.Description,
                ParentId = dto.ParentId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.TreeNodes.Add(node);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return MapToDto(node);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<NodeDto?> GetNodeAsync(int id)
    {
        var node = await _context.TreeNodes.FindAsync(id);
        return node == null ? null : MapToDto(node);
    }

    public async Task<IEnumerable<NodeDto>> GetAllNodesAsync()
    {
        var nodes = await _context.TreeNodes.ToListAsync();
        return nodes.Select(MapToDto);
    }

    public async Task<TreeNodeDto?> GetTreeAsync(int? rootId = null)
    {
        if (rootId.HasValue)
        {
            var root = await _context.TreeNodes
                .Include(n => n.Children)
                .FirstOrDefaultAsync(n => n.Id == rootId.Value);

            return root == null ? null : await BuildTreeAsync(root);
        }

        var roots = await _context.TreeNodes
            .Where(n => n.ParentId == null)
            .Include(n => n.Children)
            .ToListAsync();

        if (!roots.Any())
        {
            return null;
        }

        var virtualRoot = new TreeNodeDto
        {
            Id = 0,
            Name = "Root",
            Children = new List<TreeNodeDto>()
        };

        foreach (var root in roots)
        {
            virtualRoot.Children.Add(await BuildTreeAsync(root));
        }

        return virtualRoot;
    }

    public async Task<NodeDto?> UpdateNodeAsync(int id, UpdateNodeDto dto)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var node = await _context.TreeNodes.FindAsync(id);
            if (node == null)
            {
                return null;
            }

            if (dto.ParentId.HasValue)
            {
                if (dto.ParentId.Value == id)
                {
                    throw new InvalidOperationException("Node cannot be its own parent");
                }

                if (await WouldCreateCycleAsync(id, dto.ParentId.Value))
                {
                    throw new InvalidOperationException("Moving node would create a cycle");
                }

                var parentExists = await _context.TreeNodes
                    .AnyAsync(n => n.Id == dto.ParentId.Value);

                if (!parentExists)
                {
                    throw new InvalidOperationException("Parent node does not exist");
                }
            }

            node.Name = dto.Name;
            node.Description = dto.Description;
            node.ParentId = dto.ParentId;
            node.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return MapToDto(node);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> DeleteNodeAsync(int id)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var node = await _context.TreeNodes
                .Include(n => n.Children)
                .FirstOrDefaultAsync(n => n.Id == id);

            if (node == null)
            {
                return false;
            }

            if (node.Children.Any())
            {
                throw new InvalidOperationException("Cannot delete node with children");
            }

            _context.TreeNodes.Remove(node);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<string> ExportTreeAsync(int? rootId = null)
    {
        var tree = await GetTreeAsync(rootId);
        return System.Text.Json.JsonSerializer.Serialize(tree, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    private async Task<bool> WouldCreateCycleAsync(int nodeId, int newParentId)
    {
        var currentId = newParentId;
        var visited = new HashSet<int>();

        while (currentId != 0)
        {
            if (currentId == nodeId)
            {
                return true;
            }

            if (!visited.Add(currentId))
            {
                return true;
            }

            var parent = await _context.TreeNodes
                .Where(n => n.Id == currentId)
                .Select(n => n.ParentId)
                .FirstOrDefaultAsync();

            if (!parent.HasValue)
            {
                break;
            }

            currentId = parent.Value;
        }

        return false;
    }

    private async Task<TreeNodeDto> BuildTreeAsync(TreeNode node)
    {
        var dto = new TreeNodeDto
        {
            Id = node.Id,
            Name = node.Name,
            Description = node.Description,
            ParentId = node.ParentId,
            CreatedAt = node.CreatedAt,
            UpdatedAt = node.UpdatedAt,
            Children = new List<TreeNodeDto>()
        };

        var children = await _context.TreeNodes
            .Where(n => n.ParentId == node.Id)
            .ToListAsync();

        foreach (var child in children)
        {
            dto.Children.Add(await BuildTreeAsync(child));
        }

        return dto;
    }

    private static NodeDto MapToDto(TreeNode node)
    {
        return new NodeDto
        {
            Id = node.Id,
            Name = node.Name,
            Description = node.Description,
            ParentId = node.ParentId,
            CreatedAt = node.CreatedAt,
            UpdatedAt = node.UpdatedAt
        };
    }
}
