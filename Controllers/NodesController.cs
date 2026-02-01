using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TreeService.DTOs;
using TreeService.Models;
using TreeService.Services;

namespace TreeService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NodesController : ControllerBase
{
    private readonly ITreeService _treeService;

    public NodesController(ITreeService treeService)
    {
        _treeService = treeService;
    }

    /// <summary>
    /// Получить все узлы (плоский список)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<NodeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var nodes = await _treeService.GetAllNodesAsync();
        return Ok(nodes);
    }

    /// <summary>
    /// Получить узел по ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(NodeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var node = await _treeService.GetNodeAsync(id);
        
        if (node == null)
        {
            return NotFound(new { message = "Node not found" });
        }

        return Ok(node);
    }

    /// <summary>
    /// Получить дерево (иерархическая структура)
    /// </summary>
    [HttpGet("tree")]
    [ProducesResponseType(typeof(TreeNodeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTree([FromQuery] int? rootId = null)
    {
        var tree = await _treeService.GetTreeAsync(rootId);
        
        if (tree == null)
        {
            return NotFound(new { message = "Tree not found" });
        }

        return Ok(tree);
    }

    /// <summary>
    /// Создать новый узел
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(NodeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateNodeDto dto)
    {
        try
        {
            var node = await _treeService.CreateNodeAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = node.Id }, node);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Обновить узел (только для администраторов)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(NodeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateNodeDto dto)
    {
        try
        {
            var node = await _treeService.UpdateNodeAsync(id, dto);
            
            if (node == null)
            {
                return NotFound(new { message = "Node not found" });
            }

            return Ok(node);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Удалить узел (только для администраторов)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var result = await _treeService.DeleteNodeAsync(id);
            
            if (!result)
            {
                return NotFound(new { message = "Node not found" });
            }

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Экспортировать дерево в JSON
    /// </summary>
    [HttpGet("export")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> Export([FromQuery] int? rootId = null)
    {
        var json = await _treeService.ExportTreeAsync(rootId);
        return Content(json, "application/json");
    }
}
