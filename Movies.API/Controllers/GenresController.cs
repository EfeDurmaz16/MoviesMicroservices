#nullable disable

using CORE.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Movies.APP.Features.Genres;

namespace Movies.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class GenresController : ControllerBase
    {
        private readonly ILogger<GenresController> _logger;
        private readonly IMediator _mediator;

        public GenresController(
            ILogger<GenresController> logger,
            IMediator mediator)
        {
            _logger = logger;
            _mediator = mediator;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Get(
            CancellationToken cancellationToken)
        {
            try
            {
                var response = await _mediator.Send(
                    new GenreQueryRequest(),
                    cancellationToken);

                var list = await response.ToListAsync(
                    cancellationToken);

                if (list.Any())
                    return Ok(list);

                return NoContent();
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "GenresGet failed.");

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new CommandResponse(
                        false,
                        "An exception occurred during GenresGet."));
            }
        }

        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(
            int id,
            CancellationToken cancellationToken)
        {
            try
            {
                var response = await _mediator.Send(
                    new GenreQueryRequest(),
                    cancellationToken);

                var item = await response.SingleOrDefaultAsync(
                    genre => genre.Id == id,
                    cancellationToken);

                if (item is null)
                    return NotFound(new CommandResponse(
                        false,
                        "Genre not found!"));

                return Ok(item);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "GenresGetById failed.");

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new CommandResponse(
                        false,
                        "An exception occurred during GenresGetById."));
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Post(
            GenreAddRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var response = await _mediator.Send(
                    request,
                    cancellationToken);

                if (response.IsSuccessful)
                    return CreatedAtAction(
                        nameof(GetById),
                        new { id = response.Id },
                        response);

                return BadRequest(response);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "GenresPost failed.");

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new CommandResponse(
                        false,
                        "An exception occurred during GenresPost."));
            }
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Put(
            int id,
            GenreUpdateRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                request.Id = id;

                var response = await _mediator.Send(
                    request,
                    cancellationToken);

                if (response.IsSuccessful)
                    return Ok(response);

                if (response.Message == "Genre not found!")
                    return NotFound(response);

                return BadRequest(response);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "GenresPut failed.");

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new CommandResponse(
                        false,
                        "An exception occurred during GenresPut."));
            }
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(
            int id,
            CancellationToken cancellationToken)
        {
            try
            {
                var response = await _mediator.Send(
                    new GenreRemoveRequest
                    {
                        Id = id
                    },
                    cancellationToken);

                if (response.IsSuccessful)
                    return Ok(response);

                if (response.Message == "Genre not found!")
                    return NotFound(response);

                return BadRequest(response);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "GenresDelete failed.");

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new CommandResponse(
                        false,
                        "An exception occurred during GenresDelete."));
            }
        }
    }
}