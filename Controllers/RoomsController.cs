#nullable disable
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

namespace HotelService.Controllers
{
    [Authorize]
    public class RoomsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IHostEnvironment _environment;

        public RoomsController(ApplicationDbContext context, UserManager<IdentityUser> userManager, IHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }

        // GET: Rooms
        public async Task<IActionResult> Index(int? id)
        {
            if (id == null || _context.Rooms == null)
            {
                return NotFound();
            }

            var rooms = await _context.Rooms.Where(r => r.HotelId == id).ToListAsync();

            if (rooms.Count > 0)
            {
                int hotelId = rooms.First().HotelId;
                string hotelOwnerId = _context.Hotels.Where(w => w.Id == hotelId).Select(s => s.UserId).First();
                string userId = _userManager.GetUserId(User);

                if (hotelOwnerId != userId)
                {
                    return Redirect("/hotels");
                }
            }


            if (rooms.Count == 0)
                return RedirectToAction("Create", "Rooms", new { idHotel = id });

            return View(rooms);
        }

        // GET: Rooms/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Rooms == null)
            {
                return NotFound();
            }

            var room = await _context.Rooms
                .FirstOrDefaultAsync(m => m.Id == id);
            if (room == null)
            {
                return NotFound();
            }

            return View(room);
        }

        // GET: Rooms/Create
        [Authorize]
        [Route("Rooms/Create/{idHotel:int}")]
        public async Task<IActionResult> Create(int? idHotel)
        {
            if (idHotel == null || _context.Hotels == null)
            {
                return NotFound();
            }

            var hotel = await _context.Hotels.FirstOrDefaultAsync(h => h.Id == idHotel);

            if (hotel == null)
                return NotFound();

            var userId = _userManager.GetUserId(User);

            if (hotel.UserId != userId)
            {
                return Forbid();
            }

            return View();
        }

        // POST: Rooms/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Rooms/Create/{idHotel:int}")]
        public async Task<IActionResult> Create(int idHotel, [Bind("Id,Name,Description,Price,People, RoomPhoto, CountOfRooms")] Room room)
        {
            room.HotelId = idHotel;
            room.FolderName = room.Name;
            if (ModelState.IsValid)
            {
                if (room.RoomPhoto == null)
                {
                    ModelState.AddModelError("RoomPhoto", "Nie wybrano zdjęcia pokoju.");
                    return View(room);
                }

                this.UploadRoomPhoto(room, idHotel);

                _context.Add(room);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index", "Rooms", new { id = room.HotelId });
            }
            return View(room);
        }

        // GET: Rooms/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Hotels == null || _context.Rooms == null)
            {
                return NotFound();
            }

            var room = await _context.Rooms.FindAsync(id);
            TempData["Test"] = room.FolderName;

            if (room == null)
            {
                return NotFound();
            }


            int hotelId = room.HotelId;
            string hotelOwnerId = _context.Hotels.Where(w => w.Id == hotelId).Select(s => s.UserId).First();
            string userId = _userManager.GetUserId(User);

            if (hotelOwnerId != userId)
            {
                return Redirect("/hotels");
            }

            return View(room);
        }

        // POST: Rooms/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Price,People,HotelId,RoomPhoto, CountOfRooms")] Room room)
        {
            if (id != room.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    room.FolderName = TempData["Test"].ToString();

                    if (room.RoomPhoto != null)
                    {
                        this.UploadRoomPhoto(room, room.HotelId);
                    }

                    _context.Update(room);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RoomExists(room.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("Index", "Rooms", new { id = room.HotelId });
            }
            return View(room);
        }

        // GET: Rooms/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Rooms == null)
            {
                return NotFound();
            }

            var room = await _context.Rooms.FirstOrDefaultAsync(m => m.Id == id);

            if (room == null)
            {
                return NotFound();
            }

            int hotelId = room.HotelId;
            string hotelOwnerId = _context.Hotels.Where(w => w.Id == hotelId).Select(s => s.UserId).First();
            string userId = _userManager.GetUserId(User);

            if (hotelOwnerId != userId)
            {
                return Redirect("/hotels");
            }

            return View(room);
        }

        // POST: Rooms/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Rooms == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Rooms'  is null.");
            }
            var room = await _context.Rooms.FindAsync(id);
            
            if (room != null)
            {
                _context.Rooms.Remove(room);
            }

            var hotelName = _context.Hotels.Find(room.HotelId).FolderName;

            string FolderName = _environment.ContentRootPath;
            FolderName = _environment.ContentRootPath.Remove(FolderName.Length - 2);
            string FolderPath = FolderName.Substring(0, FolderName.LastIndexOf('\\'));

            string path = Path.Combine(FolderPath, $"Hotels\\{hotelName}\\Rooms\\{room.FolderName}");

            Directory.Delete(path, true);

            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Rooms", new { id = room.HotelId });
        }

        private bool RoomExists(int id)
        {
            return (_context.Rooms?.Any(e => e.Id == id)).GetValueOrDefault();
        }

        private void UploadRoomPhoto(Room room, int idHotel)
        {
            var hotelName = _context.Hotels.Find(idHotel).FolderName;

            string FolderName = _environment.ContentRootPath;
            FolderName = _environment.ContentRootPath.Remove(FolderName.Length - 2); //removing last //
            string FolderPath = FolderName.Substring(0, FolderName.LastIndexOf('\\')); //without last hotelService

            string path = Path.Combine(FolderPath, $"Hotels\\{hotelName}\\Rooms\\{room.FolderName}");

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            FileInfo fileInfo = new FileInfo(room.RoomPhoto.FileName);
            if (!fileInfo.Extension.ToLower().Equals(".jpg") && !fileInfo.Extension.ToLower().Equals(".jpeg") && !fileInfo.Extension.ToLower().Equals(".png"))
            {
                ModelState.AddModelError("IncorrectExtensionError", "Nie prawidłowy format pliku - akceptowalne rozszerzenia to: png/jpg/jpeg");
                room.RoomPhoto = null;
            }
            else
            {
                string fileName = "index.jpg";
                string fileNameWithPath = Path.Combine(path, fileName);

                using (var stream = new FileStream(fileNameWithPath, FileMode.Create))
                {
                    room.RoomPhoto.CopyTo(stream);
                }

            }

        }
    }
}
