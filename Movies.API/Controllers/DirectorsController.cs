using CORE.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Movies.APP.Features.Directors;

namespace Movies.API.Controllers
{
    [Route("api/directors")]
    [ApiController]
    public class DirectorsController : ControllerBase
    {
        private readonly ILogger<DirectorsController> _logger;
        private readonly IMediator _mediator;

        public DirectorsController(ILogger<DirectorsController> logger, IMediator mediator)
        {
            _logger = logger;
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> Get(
            [FromQuery] DirectorQueryRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _mediator.Send(request, cancellationToken);
                var list = await response.ToListAsync(cancellationToken);
                return Ok(list);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "DirectorsGet failed.");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new CommandResponse(false, "An exception occurred during DirectorsGet."));
            }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _mediator.Send(new DirectorQueryRequest(), cancellationToken);
                var item = await response.SingleOrDefaultAsync(d => d.Id == id, cancellationToken);
                if (item is null)
                    return NotFound(new CommandResponse(false, "Director not found!"));

                return Ok(item);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "DirectorsGetById failed.");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new CommandResponse(false, "An exception occurred during DirectorsGetById."));
            }
        }

        [HttpGet("{id:int}/movies")]
        public async Task<IActionResult> GetMovies(int id, CancellationToken cancellationToken)
        {
            try
            {
                var directors = await _mediator.Send(new DirectorQueryRequest(), cancellationToken);
                var exists = await directors.AnyAsync(d => d.Id == id, cancellationToken);
                if (!exists)
                    return NotFound(new CommandResponse(false, "Director not found!"));

                var response = await _mediator.Send(
                    new DirectorMoviesQueryRequest { Id = id }, cancellationToken);
                var list = await response.ToListAsync(cancellationToken);
                return Ok(list);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "DirectorsGetMovies failed.");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new CommandResponse(false, "An exception occurred during DirectorsGetMovies."));
            }
        }

        [HttpPost]
        public async Task<IActionResult> Post(DirectorAddRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _mediator.Send(request, cancellationToken);
                if (!response.IsSuccessful)
                    return BadRequest(response);

                return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "DirectorsPost failed.");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new CommandResponse(false, "An exception occurred during DirectorsPost."));
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Put(int id, DirectorUpdateRequest request, CancellationToken cancellationToken)
        {
            try
            {
                request.Id = id;
                var response = await _mediator.Send(request, cancellationToken);
                if (response.IsSuccessful)
                    return Ok(response);

                if (response.Message == "Director not found!")
                    return NotFound(response);

                return BadRequest(response);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "DirectorsPut failed.");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new CommandResponse(false, "An exception occurred during DirectorsPut."));
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _mediator.Send(new DirectorRemoveRequest { Id = id }, cancellationToken);
                if (response.IsSuccessful)
                    return NoContent();

                if (response.Message == "Director has related movies!")
                    return Conflict(response);

                return NotFound(response);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "DirectorsDelete failed.");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new CommandResponse(false, "An exception occurred during DirectorsDelete."));
            }
        }
    }
}
