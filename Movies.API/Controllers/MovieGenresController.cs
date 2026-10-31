#nullable disable
using CORE.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Movies.APP.Features.MovieGenres;

namespace Movies.API.Controllers
{
    [Route("api/movie-genres")]
    [ApiController]
    public class MovieGenresController : ControllerBase
    {
        private readonly ILogger<MovieGenresController> _logger;
        private readonly IMediator _mediator;

        public MovieGenresController(
            ILogger<MovieGenresController> logger, IMediator mediator)
        {
            _logger = logger;
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> Get(
            [FromQuery] MovieGenreQueryRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var response = await _mediator.Send(request, cancellationToken);
                var list = await response.ToListAsync(cancellationToken);
                return Ok(list);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "MovieGenresGet failed.");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new CommandResponse(false,
                        "An exception occurred during MovieGenresGet."));
            }
        }

        [HttpGet("{movieId:int}/{genreId:int}")]
        public async Task<IActionResult> GetById(
            int movieId, int genreId, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _mediator.Send(
                    new MovieGenreQueryRequest
                    {
                        MovieId = movieId,
                        GenreId = genreId
                    }, cancellationToken);

                var item = await response.SingleOrDefaultAsync(cancellationToken);
                if (item is null)
                    return NotFound(new CommandResponse(false,
                        "MovieGenre not found!"));

                return Ok(item);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "MovieGenresGetById failed.");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new CommandResponse(false,
                        "An exception occurred during MovieGenresGetById."));
            }
        }

        [HttpPost]
        public async Task<IActionResult> Post(
            MovieGenreAddRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _mediator.Send(request, cancellationToken);
                if (response.IsSuccessful)
                    return CreatedAtAction(nameof(GetById),
                        new
                        {
                            movieId = request.MovieId.Value,
                            genreId = request.GenreId.Value
                        }, response);

                if (response.Message == "Movie not found!" ||
                    response.Message == "Genre not found!")
                    return NotFound(response);

                if (response.Message == "MovieGenre already exists!")
                    return Conflict(response);

                return BadRequest(response);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "MovieGenresPost failed.");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new CommandResponse(false,
                        "An exception occurred during MovieGenresPost."));
            }
        }

        [HttpPut("{movieId:int}/{genreId:int}")]
        public async Task<IActionResult> Put(
            int movieId, int genreId, MovieGenreUpdateRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                request.OldMovieId = movieId;
                request.OldGenreId = genreId;

                var response = await _mediator.Send(request, cancellationToken);
                if (response.IsSuccessful)
                    return Ok(response);

                if (response.Message == "MovieGenre not found!" ||
                    response.Message == "Movie not found!" ||
                    response.Message == "Genre not found!")
                    return NotFound(response);

                if (response.Message == "MovieGenre already exists!")
                    return Conflict(response);

                return BadRequest(response);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "MovieGenresPut failed.");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new CommandResponse(false,
                        "An exception occurred during MovieGenresPut."));
            }
        }

        [HttpDelete("{movieId:int}/{genreId:int}")]
        public async Task<IActionResult> Delete(
            int movieId, int genreId, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _mediator.Send(
                    new MovieGenreRemoveRequest
                    {
                        MovieId = movieId,
                        GenreId = genreId
                    }, cancellationToken);

                if (response.IsSuccessful)
                    return NoContent();

                return NotFound(response);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "MovieGenresDelete failed.");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new CommandResponse(false,
                        "An exception occurred during MovieGenresDelete."));
            }
        }
    }
}
