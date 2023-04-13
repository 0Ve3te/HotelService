using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HotelService.Data;
using HotelService.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Twilio;
using Twilio.Rest.Verify.V2.Service;

namespace HotelService.Controllers
{
    [Authorize]
    public class ReservationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailSender _emailSender;

        public ReservationsController(ApplicationDbContext context, UserManager<IdentityUser> userManager, IEmailSender emailSender)
        {
            _context = context;
            _userManager = userManager;
            _emailSender = emailSender;
        }

        // GET: Reservations
        public async Task<IActionResult> Index()
        {
            var UserId = _userManager.GetUserId(User);
            List<Reservation> reservations = await _context.Reservations.Where(r => r.UserId == UserId).ToListAsync();

            foreach (Reservation reservation in reservations)
            {
                reservation.HotelName = _context.Hotels.Find(reservation.HotelId).Name;
            }


            return View(reservations);
        }

        // GET: Reservations
        public async Task<IActionResult> Hotel(int id)
        {
            var UserId = _userManager.GetUserId(User);
            var hotel = _context.Hotels.Where(h => h.Id == id).FirstOrDefault();

            if (hotel == null)
                return NotFound();

            if (hotel.UserId != UserId)
                return NotFound();

            List<Reservation> reservations = await _context.Reservations.Where(r => r.HotelId == id).ToListAsync();

            foreach (Reservation reservation in reservations)
            {
                reservation.RoomName = _context.Rooms.Find(reservation.RoomId).Name;
            }

            ViewData["HotelName"] = hotel.Name;
            ViewData["HotelId"] = hotel.Id;

            return View(reservations);
        }

        // GET: Reservations/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Reservations == null)
            {
                return NotFound();
            }

            var reservation = await _context.Reservations
                .FirstOrDefaultAsync(m => m.Id == id);

            if (reservation == null)
            {
                return NotFound();
            }

            reservation.HotelName = _context.Hotels.Find(reservation.HotelId).Name;
            reservation.RoomName = _context.Rooms.Find(reservation.RoomId).Name;


            return View(reservation);
        }

        // GET: Reservations/Create
        [Route("Reservations/Create/{hotelId:int}")]
        public IActionResult Create(int hotelId)
        {
            if (hotelId == null || _context.Hotels == null)
            {
                return NotFound();
            }

            var hotel = _context.Hotels.Find(hotelId);

            ViewData["HotelName"] = hotel.Name;
            ViewData["HotelId"] = hotel.Id;
            ViewData["HotelRooms"] = new SelectList(_context.Rooms.Where(r => r.HotelId == hotelId), "Id", "Name");
            ViewData["RoomPrices"] = new SelectList(_context.Rooms.Where(r => r.HotelId == hotelId), "Id", "Price");

            return View();
        }

        // POST: Reservations/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Reservations/Create/{hotelId:int}")]
        public async Task<IActionResult> Create(int hotelid, [Bind("Id,DateStart,DateEnd,FirstName,LastName,PhoneNumber,Email,RoomId, CodeSMS")] Reservation reservation)
        {
            reservation.HotelId = hotelid;
            reservation.UserId = _userManager.GetUserId(User);
            reservation.Price = 0;

            if(reservation.DateEnd < reservation.DateStart)
                ModelState.AddModelError("IncorrectDateEnd", "Wybrana data jest nieprawidłowa!");
            if(reservation.DateStart > reservation.DateEnd)
                ModelState.AddModelError("IncorrectDateStart", "Wybrana data jest nieprawidłowa!");

            if (ModelState.IsValid)
            {
                var verification = this.CheckSmsCode(reservation.CodeSMS, $"+48{reservation.PhoneNumber}");

                if(verification.IsCompletedSuccessfully == true)
                {
                    var dayCount = (reservation.DateEnd - reservation.DateStart).TotalDays;
                    if (dayCount == 0)
                        dayCount = 1;
                    var paymentPerNight = _context.Rooms.Find(reservation.RoomId).Price;

                    reservation.Price = ((int)dayCount) * paymentPerNight;

                    _context.Add(reservation);
                    await _context.SaveChangesAsync();

                    //email to hotel owner
                    var ownerId = _context.Hotels.Where(h => h.Id == hotelid).Select(s => s.UserId).FirstOrDefault();
                    var owner = _context.Users.Where(w => w.Id == ownerId).FirstOrDefault();

                    string body = $"<div style='text-align: center;'><img src='https://i.imgur.com/sIoHYch.png' alt='logo-apartamenty.pl' width='200'><h1 style='background-color: royalblue; padding: 20px; color:white;'>Witaj, {owner.NormalizedUserName} !</h1><p>Właśnie została zarejestrowana nowa rezerwacja w Twoim hotelu!</p><p>Więcej informacji możesz przejrzeć w zakładce zarządzania lub klikając w poniższy link.</p><p><a style='color: white; text-decoration: none; background-color: orangered; padding: 10px; border-radius: 5px; font-family: arial;' href='https://localhost:44361/Reservations/ChangeStatus/{reservation.Id}'>Przejdź do szczegółów</a></p></div><p style='text-align: right;'>Pozdrawiamy <br> <i>Zespół Apartament.pl</i></p>";
                    await _emailSender.SendEmailAsync(owner.Email, "Nowa rezerwacja!", body);

                    //email to client
                    var roomName = _context.Rooms.Where(w => w.Id == reservation.RoomId).Select(s => s.Name).FirstOrDefault();
                    var hotelName = _context.Hotels.Where(w => w.Id == reservation.HotelId).Select(s => s.Name).FirstOrDefault();
                    body = $"<div style='text-align: center;'><img src='https://i.imgur.com/sIoHYch.png' alt='logo-apartamenty.pl' width='200'><h1 style='background-color: royalblue; padding: 20px; color:white;'>Witaj, {owner.NormalizedUserName} !</h1><p>Właśnie został rozpoczęty proces Twojej rezerwacji pokoju: {roomName} w {hotelName}!</p><p>Aktualny status Twojej rezerwacji, to: <i>{reservation.Status}</i>. O wszystkich aktualizacjach będziemy powiadamiali Cię mailowo.</p><p>Więcej informacji możesz przejrzeć w panelu historii rezerwacji lub bezpośrednio klikając w poniższy link.</p><p><a style='color: white; text-decoration: none; background-color: orangered; padding: 10px; border-radius: 5px; font-family: arial;' href='https://localhost:44361/Reservations/Details/{reservation.Id}'>Przejdź do szczegółów</a></p></div><p style='text-align: right;'>Pozdrawiamy <br> <i>Zespół Apartament.pl</i></p>";
                    await _emailSender.SendEmailAsync(reservation.Email, "Rezerwacja rozpoczęta!", body);

                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    ModelState.AddModelError("IncorrectVerificationCode", "Podano nieprawidłowy kod weryfikacyjny!");
                    ViewData["HotelId"] = hotelid;
                    ViewData["RoomPrices"] = new SelectList(_context.Rooms.Where(r => r.HotelId == hotelid), "Id", "Price");
                    ViewData["HotelName"] = _context.Hotels.Find(hotelid).Name;
                    ViewData["HotelRooms"] = new SelectList(_context.Rooms.Where(r => r.HotelId == hotelid), "Id", "Name");
                    return View(reservation);
                }
            }

            ViewData["HotelId"] = hotelid;
            ViewData["RoomPrices"] = new SelectList(_context.Rooms.Where(r => r.HotelId == hotelid), "Id", "Price");
            ViewData["HotelName"] = _context.Hotels.Find(hotelid).Name;
            ViewData["HotelRooms"] = new SelectList(_context.Rooms.Where(r => r.HotelId == hotelid), "Id", "Name");
            return View(reservation);
        }

        // GET: Reservations/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Reservations == null)
            {
                return NotFound();
            }

            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null)
            {
                return NotFound();
            }
            return View(reservation);
        }

        // POST: Reservations/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,UserId,DateStart,DateEnd,FirstName,LastName,PhoneNumber,Email,Price")] Reservation reservation)
        {
            if (id != reservation.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(reservation);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ReservationExists(reservation.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(reservation);
        }

        // GET: Reservations/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Reservations == null)
            {
                return NotFound();
            }

            var reservation = await _context.Reservations
                .FirstOrDefaultAsync(m => m.Id == id);
            if (reservation == null)
            {
                return NotFound();
            }

            return View(reservation);
        }

        // POST: Reservations/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Reservations == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Reservations'  is null.");
            }
            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation != null)
            {
                _context.Reservations.Remove(reservation);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> ChangeStatus(int id)
        {
            if (id == null || _context.Reservations == null)
            {
                return NotFound();
            }

            var reservation = await _context.Reservations.FirstOrDefaultAsync(m => m.Id == id);

            if (reservation == null)
            {
                return NotFound();
            }

            reservation.HotelName = _context.Hotels.Find(reservation.HotelId).Name;
            reservation.RoomName = _context.Rooms.Find(reservation.RoomId).Name;

            return View(reservation);
        }

        [HttpPost]
        public async Task<IActionResult> ChangeStatus(int id, [Bind("Status")] Reservation reservation)
        {
            if (id == null || _context.Reservations == null)
            {
                return NotFound();
            }

            var getReservation = await _context.Reservations.FirstOrDefaultAsync(m => m.Id == id);

            if (reservation == null)
            {
                return NotFound();
            }

            getReservation.Status = reservation.Status;
            _context.Update(getReservation);
            await _context.SaveChangesAsync();

            string body = $"  <div style='text-align: center;'><img src='https://i.imgur.com/sIoHYch.png' alt='logo-apartamenty.pl' width='200'><h1 style='background-color: royalblue; padding: 20px; color: white; '>Witaj, {getReservation.FirstName} {getReservation.LastName} !</h1><p>Status Twojej rezerwacji został właśnie zaktualizowany!</p><p>Nowy status Twojej rezerwacji, to: {getReservation.Status}.</p><p>Jeśli uważasz, że wystąpił jakiś błąd skontaktuj się bezpośrednio z obsługą hotelu.</p></div><p style='text-align: right;'>Pozdrawiamy <br> <i>Zespół Apartament.pl</i></p>";
            await _emailSender.SendEmailAsync(getReservation.Email, "Zmiana statusu rezerwacji!", body);

            return RedirectToAction("Manage", "Hotels");
        }

        private bool ReservationExists(int id)
        {
          return (_context.Reservations?.Any(e => e.Id == id)).GetValueOrDefault();
        }

        [HttpGet]
        public async Task<IActionResult> GetActiveReservations(int hotelId)
        {
            var today = DateTime.Today.Date;
            var data = await _context.Reservations.Where(r => r.HotelId == hotelId && r.DateEnd > today).ToListAsync();
            return Ok(data);
        }

        [HttpGet]
        public async Task<IActionResult> GetUserReservations()
        {
            var UserId = _userManager.GetUserId(User);
            var today = DateTime.Today.Date;
            var data = await _context.Reservations.Where(r => r.UserId == UserId && r.DateEnd > today).ToListAsync();
            return Ok(data);
        }

        //Twilio verification
        [HttpGet]
        public async Task<IActionResult> SendSMS(string number)
        {
            string accountSid = "AC4856b8af6816928e776386aff459a6b8";
            string authToken = "587126b36e83f9938fb41021aea2db07";

            TwilioClient.Init(accountSid, authToken);

            var verification = VerificationResource.Create(
                to: number,
                channel: "sms",
                pathServiceSid: "VAb4a4e3649e2cfe0b0c661095bc229783"
           );

            return Ok(verification.Status);
        }

        [HttpGet]
        public async Task<IActionResult> CheckSmsCode(string code, string number)
        {
            string accountSid = "AC4856b8af6816928e776386aff459a6b8";
            string authToken = "587126b36e83f9938fb41021aea2db07";

            TwilioClient.Init(accountSid, authToken);

            var verificationCheck = VerificationCheckResource.Create(
                to: number,
                code: code,
                pathServiceSid: "VAb4a4e3649e2cfe0b0c661095bc229783"
            );

            return Ok(verificationCheck.Valid);
        }

        [HttpGet]
        public async Task<IActionResult> GetUnavailableDays(int roomId)
        {
            DateTime today = DateTime.Now.Date;
            List<DateTime> duplicateDays = GetDuplicateDays(roomId, today);
            List<String> results = new List<String>();

            duplicateDays.ForEach(e =>
            {
                results.Add(e.ToShortDateString());
            });

            return Ok(results);
        }

        [HttpGet]
        public async Task<IActionResult> GetEndDay(int roomId = 4, string startDay = "11.08.2022")
        {
            var startingDay = DateTime.Parse(startDay);
            List<DateTime> duplicateDays = GetDuplicateDays(roomId, startingDay);
            List<String> results = new List<String>();

            duplicateDays.ForEach(e =>
            {
               e = e.AddDays(-1);
                results.Add(e.ToShortDateString());
            });

            return Ok(results.Min());
        }

        private List<DateTime> GetDuplicateDays(int roomId, DateTime startingDay)
        {
            int reservationsLimit = _context.Rooms.Where(r => r.Id == roomId).Select(s => s.CountOfRooms).FirstOrDefault();
            var reservations = _context.Reservations.Where(r => r.RoomId == roomId && r.DateStart >= startingDay).Select(s => new { DateStart = s.DateStart.ToShortDateString(), DateEnd = s.DateEnd.ToShortDateString() }).ToList();

            List<DateTime> daysList = new List<DateTime>();

            reservations.ForEach(e =>
            {
                var startingDate = DateTime.Parse(e.DateStart);
                var endingDate = DateTime.Parse(e.DateEnd);

                for (DateTime date = startingDate; date <= endingDate; date = date.AddDays(1))
                {
                    daysList.Add(date);
                }
            });

            List<DateTime> duplicateDays = daysList.GroupBy(x => x).Where(g => g.Count() >= reservationsLimit).Select(x => x.Key).ToList();
            return duplicateDays;
        }
    }
}
