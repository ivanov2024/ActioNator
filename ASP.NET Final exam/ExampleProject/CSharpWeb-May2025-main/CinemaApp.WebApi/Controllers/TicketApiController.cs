namespace CinemaApp.WebApi.Controllers
{
    using System.ComponentModel.DataAnnotations;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    using Services.Core.Interfaces;
    
    public class TicketApiController : BaseExternalApiController
    {
        private readonly ITicketService ticketService;

        public TicketApiController(ITicketService ticketService)
        {
            this.ticketService = ticketService;
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Route("Buy")]
        [Authorize]
        public async Task<ActionResult> BuyTicket([Required]string cinemaId, 
            [Required]string movieId, int quantity, [Required]string showtime)
        {
            string? userId = this.GetUserId();
            bool result = await this.ticketService
                .AddTicketAsync(cinemaId, movieId, quantity, showtime, userId);
            if (result == false)
            {
                return this.BadRequest();
            }

            return this.Ok();
        }
    }
}
