using CORE.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Movies.APP.Features.Movies;

namespace Movies.API.Controllers
{
    [Route("api/movies")]
    [ApiController]
    public class MoviesController : ControllerBase
    {
        private readonly ILogger<MoviesController> _logger;
        private readonly IMediator _mediator;

        public MoviesController(ILogger<MoviesController> logger, IMediator mediator)
        {
            _logger = logger;
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> Get(CancellationToken cancellationToken)
        {
            try
            {
                var response = await _mediator.Send(new MovieQueryRequest(), cancellationToken);
                var list = await response.ToListAsync(cancellationToken);
                return Ok(list);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "MoviesGet failed.");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new CommandResponse(false, "An exception occurred during MoviesGet."));
            }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _mediator.Send(new MovieQueryRequest(), cancellationToken);
                var item = await response.SingleOrDefaultAsync(m => m.Id == id, cancellationToken);
                if (item is null)
                    return NotFound(new CommandResponse(false, "Movie not found!"));

                return Ok(item);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "MoviesGetById failed.");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new CommandResponse(false, "An exception occurred during MoviesGetById."));
            }
        }

        [HttpPost]
        public async Task<IActionResult> Post(MovieAddRequest request, CancellationToken cancellationToken)
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
                _logger.LogError(exception, "MoviesPost failed.");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new CommandResponse(false, "An exception occurred during MoviesPost."));
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Put(int id, MovieUpdateRequest request, CancellationToken cancellationToken)
        {
            try
            {
                request.Id = id;
                var response = await _mediator.Send(request, cancellationToken);
                if (response.IsSuccessful)
                    return Ok(response);

                if (response.Message == "Movie not found!")
                    return NotFound(response);

                return BadRequest(response);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "MoviesPut failed.");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new CommandResponse(false, "An exception occurred during MoviesPut."));
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _mediator.Send(new MovieRemoveRequest { Id = id }, cancellationToken);
                if (response.IsSuccessful)
                    return NoContent();

                return NotFound(response);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "MoviesDelete failed.");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new CommandResponse(false, "An exception occurred during MoviesDelete."));
            }
        }
    }
}
