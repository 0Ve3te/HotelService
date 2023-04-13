using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HotelService.Data;
using HotelService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace HotelService.Views
{
    public class ReviewsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ReviewsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Reviews
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Reviews.Include(r => r.Hotel);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Reviews/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Reviews == null)
            {
                return NotFound();
            }

            var review = await _context.Reviews
                .Include(r => r.Hotel)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (review == null)
            {
                return NotFound();
            }

            return View(review);
        }

        // GET: Reviews/Create
        [Authorize]
        [Route("Hotels/{idHotel:int}/Reviews/Create")]
        public IActionResult Create(int idHotel)
        {
            if (_context.Hotels == null)
            {
                return NotFound();
            }

            var hotel = _context.Hotels.Where(h => h.Id == idHotel).FirstOrDefault();

            if(hotel == null)
                return NotFound();

            var userId = _userManager.GetUserId(User);
            var today = DateTime.Today.Date;
            var results = _context.Reservations.Where(r => r.UserId == userId && r.HotelId == idHotel && r.IsRated == false && r.DateEnd <= today).Select(s => new
            {
                Id = s.Id,
                Description = $"Pobyt od {s.DateStart.ToString("yyyy-MM-dd")} do {s.DateEnd.ToString("yyyy-MM-dd")}" 
            }).ToList();
           
            if (results.Count == 0)
                ViewBag.Info = "Error";
            else
                ViewBag.Reservations = new SelectList(results, "Id", "Description");

            ViewData["HotelId"] = idHotel;
            return View();
        }

        // POST: Reviews/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        [Route("Hotels/{idHotel:int}/Reviews/Create")]
        public async Task<IActionResult> Create(int idHotel, [Bind("ReservationId, Id,FirstName,LastName,Rate,Title,Description,Pluses,Minuses")] Review review)
        {
            review.HotelId = idHotel;
            DateTime date = DateTime.Now;
            review.Date = date;
            review.UserId = _userManager.GetUserId(User);

            if (ModelState.IsValid)
            {
                _context.Add(review);
                await _context.SaveChangesAsync();
                _context.Reservations.Where(r => r.Id == review.ReservationId).FirstOrDefault().IsRated = true;
                _context.SaveChanges();
                return Redirect("/Hotels/Details/" + review.HotelId);
            }


            ViewData["HotelId"] = idHotel;
            return View(review);
        }

        // GET: Reviews/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Reviews == null)
            {
                return NotFound();
            }

            var review = await _context.Reviews.FindAsync(id);
            if (review == null)
            {
                return NotFound();
            }
            ViewData["HotelId"] = review.HotelId;
            return View(review);
        }

        // POST: Reviews/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FirstName,LastName,Rate,Title,Date,Description,Pluses,Minuses,HotelId")] Review review)
        {
            if (id != review.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    review.UserId = _userManager.GetUserId(User);
                    _context.Update(review);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ReviewExists(review.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return Redirect("/Hotels/Details/" + review.HotelId);
            }
            ViewData["HotelId"] = review.HotelId;
            return View(review);
        }

        // GET: Reviews/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Reviews == null)
            {
                return NotFound();
            }

            var review = await _context.Reviews
                .Include(r => r.Hotel)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (review == null)
            {
                return NotFound();
            }

            return View(review);
        }

        // POST: Reviews/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Reviews == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Reviews'  is null.");
            }
            var review = await _context.Reviews.FindAsync(id);
            if (review != null)
            {
                _context.Reviews.Remove(review);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ReviewExists(int id)
        {
          return (_context.Reviews?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
