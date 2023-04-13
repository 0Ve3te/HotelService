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
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace HotelService.Controllers
{
    public class HotelsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly IHostEnvironment _environment;


        public HotelsController(ApplicationDbContext context, UserManager<IdentityUser> userManager, IEmailSender emailSender, IHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _emailSender = emailSender;
            _environment = environment;
        }

        // GET: Hotels
        public IActionResult Index()
        {
            List<Hotel> hotels = _context.Hotels.ToList();
            List<Feature> features = _context.Features.ToList();
            List<FeatureHotel> FeatureHotels = _context.FeatureHotels.ToList();
            List<Room> Rooms = _context.Rooms.ToList();
            List<Review> Reviews = _context.Reviews.ToList();

            foreach (var hotel in hotels)
            {
                foreach (var fh in FeatureHotels)
                {
                    if (hotel.Id == fh.HotelId)
                    {
                        foreach (var feature in features)
                        {
                            if (feature.Id == fh.FeatureId)
                            {
                                hotel.FeatureList.Add(feature);
                            }
                        }
                    }
                }

                var revCounter = Reviews.Where(r => r.HotelId == hotel.Id).Count();

                if (revCounter > 9)
                {
                    foreach (var review in Reviews.Where(r => r.HotelId == hotel.Id))
                    {
                        hotel.AvgReviews += review.Rate;
                    }
                    hotel.AvgReviews = hotel.AvgReviews / revCounter;
                    hotel.AvgReviews = Math.Round(hotel.AvgReviews, 1);
                }

                var hotelId = _context.Rooms.Where(r => r.HotelId == hotel.Id).FirstOrDefault();

                if (hotelId != null)
                    hotel.RoomLowestPrice = Rooms.Where(r => r.HotelId == hotel.Id).Select(s => s.Price).Min();

            }

            return View(hotels);
        }

        // GET: Hotels/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var hotel = await _context.Hotels
                .FirstOrDefaultAsync(m => m.Id == id);
            if (hotel == null)
            {
                return NotFound();
            }

            string FolderName = _environment.ContentRootPath;
            FolderName = _environment.ContentRootPath.Remove(FolderName.Length - 2);
            string FolderPath = FolderName.Substring(0, FolderName.LastIndexOf('\\'));

            string filePath = Path.Combine(FolderPath, $"Hotels\\{hotel.FolderName}");

            string[] filePaths = Directory.GetFiles(filePath);

            foreach (var path in filePaths)
            {
                var tmpPath = path.Substring(path.IndexOf(@"Hotels"));
                hotel.ImagesSrc.Add(tmpPath);
            }

            var features = await _context.FeatureHotels.Where(x => x.HotelId == id).ToListAsync();
            foreach (var feature in features)
            {
                var data = _context.Features.Where(x => x.Id == feature.FeatureId).FirstOrDefault();

                hotel.FeatureList.Add(data);

            }

            hotel.RoomList = await _context.Rooms.Where(r => r.HotelId == hotel.Id).ToListAsync();
            hotel.Reviews = _context.Reviews.Where(r => r.HotelId == hotel.Id).OrderByDescending(o => o.Date).ToList();

            foreach (var review in hotel.Reviews)
            {
                var reservation = _context.Reservations.Where(x => x.Id == review.ReservationId).FirstOrDefault();
                review.ReservationStart = reservation.DateStart;
                review.ReservationEnd = reservation.DateEnd;
            }

            if (hotel.Reviews.Count > 9)
            {
                foreach (var review in hotel.Reviews)
                {
                    hotel.AvgReviews += review.Rate;
                }

                hotel.AvgReviews = hotel.AvgReviews / hotel.Reviews.Count;
                hotel.AvgReviews = Math.Round(hotel.AvgReviews, 1);
            }
            else
            {
                hotel.AvgReviews = 0;
            }

            return View(hotel);
        }

        // GET: Hotels/Create
        [Authorize]
        public IActionResult Create()
        {
            ViewData["FeaturesId"] = new SelectList(_context.Features, "Id", "Name");
            return View();
        }

        // POST: Hotels/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id, Name, Description, Country, Region, City, FeaturesIdList, HotelPhoto, HotelPhotos, GoogleMapsLocation")] Hotel hotel)
        {
            if (ModelState.IsValid)
            {
                hotel.UserId = _userManager.GetUserId(User);
                hotel.FolderName = hotel.Name;

                //---- Main img file upload ---
                this.UploadMainPhoto(hotel);

                if (hotel.HotelPhoto == null)
                {
                    ViewData["FeaturesId"] = new SelectList(_context.Features, "Id", "Name");
                    return View(hotel);
                }

                //---- Hotel files upload ---
                this.UploadPhotos(hotel);

                if (hotel.HotelPhotos == null)
                {
                    ViewData["FeaturesId"] = new SelectList(_context.Features, "Id", "Name");
                    return View(hotel);
                }

                _context.Add(hotel);
                await _context.SaveChangesAsync();

                foreach (var featureId in hotel.FeaturesIdList)
                {
                    FeatureHotel tmpFeature = new FeatureHotel();
                    tmpFeature.FeatureId = featureId;
                    tmpFeature.HotelId = hotel.Id;
                    _context.FeatureHotels.Add(tmpFeature);
                    await _context.SaveChangesAsync();
                }


                return RedirectToAction("Create", "Rooms", new { idHotel = hotel.Id });
            }
            ViewData["FeaturesId"] = new SelectList(_context.Features, "Id", "Name");
            return View(hotel);
        }

        // GET: Hotels/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var hotel = await _context.Hotels.FindAsync(id);
            if (hotel == null)
            {
                return NotFound();
            }

            string userId = _userManager.GetUserId(User);
            string userIdDb = hotel.UserId;
            if (userIdDb != userId)
            {
                return RedirectToAction("Index");
            }

            ViewBag.FeaturesId = new SelectList(_context.Features, "Id", "Name");
            var featuresId = _context.FeatureHotels.Where(h => h.HotelId == id).Select(f => f.FeatureId).ToList();
            hotel.FeaturesIdList = featuresId;

            return View(hotel);
        }

        // POST: Hotels/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Country,Region,City,GoogleMapsLocation,UserId,FeaturesIdList,HotelPhoto,HotelPhotos, FolderName")] Hotel hotel)
        {
            if (id != hotel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    string userId = _userManager.GetUserId(User);
                    string userIdDb = hotel.UserId;
                    if (userIdDb != userId)
                    {
                        ViewData["FeaturesId"] = new SelectList(_context.Features, "Id", "Name");
                        return View(hotel);
                    }

                    if (hotel.HotelPhoto != null)
                    {
                        this.DeletePhotos(hotel, true);
                        this.UploadMainPhoto(hotel, false);
                    }

                    if (hotel.HotelPhotos != null)
                    {
                        this.DeletePhotos(hotel, false);
                        this.UploadPhotos(hotel, false);
                    }

                    _context.Update(hotel);
                    await _context.SaveChangesAsync();

                    List<FeatureHotel> tmpFeatureList = _context.FeatureHotels.Where(h => h.HotelId == hotel.Id).ToList();

                    _context.RemoveRange(tmpFeatureList);
                    await _context.SaveChangesAsync();

                    foreach (var featureId in hotel.FeaturesIdList)
                    {
                        FeatureHotel tmpFeature = new FeatureHotel();
                        tmpFeature.FeatureId = featureId;
                        tmpFeature.HotelId = hotel.Id;
                        _context.FeatureHotels.Add(tmpFeature);
                        await _context.SaveChangesAsync();
                    }


                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!HotelExists(hotel.Id))
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
            ViewData["FeaturesId"] = new SelectList(_context.Features, "Id", "Name");
            return View(hotel);
        }

        // GET: Hotels/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var hotel = await _context.Hotels.FirstOrDefaultAsync(m => m.Id == id);

            if (hotel == null)
            {
                return NotFound();
            }

            string userId = _userManager.GetUserId(User);
            string userIdDb = hotel.UserId;
            if (userIdDb != userId)
            {
                return RedirectToAction("Index");
            }

            return View(hotel);
        }

        // POST: Hotels/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var hotel = await _context.Hotels.FindAsync(id);
            _context.Hotels.Remove(hotel);
            await _context.SaveChangesAsync();

            string FolderName = _environment.ContentRootPath;
            FolderName = _environment.ContentRootPath.Remove(FolderName.Length - 2);
            string FolderPath = FolderName.Substring(0, FolderName.LastIndexOf('\\'));

            string path = Path.Combine(FolderPath, $"Hotels\\{hotel.FolderName}");
            Directory.Delete(path, true);

            return RedirectToAction(nameof(Index));
        }

        // GET: Hotels/Manage
        [Authorize]
        public async Task<IActionResult> Manage()
        {
            var userId = _userManager.GetUserId(User);
            return View(await _context.Hotels.Where(h => h.UserId == userId).ToListAsync());
        }


        public async Task<IActionResult> SendEmail()
        {
            var userId = _userManager.GetUserId(User);
            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            string body = $"<div style='text-align: center;'><img src='https://i.imgur.com/sIoHYch.png' alt='logo-apartamenty.pl' width='200'><h1>Witaj, {user.NormalizedUserName} !</h1><p>Dzieli cię już tylko krok od wymarzonych wakacji!</p><p>Właśnie nastąpiła aktualizacja statusu Twojej rezerwacji w X. <br /> status realizacji: <b>w trakcie.</b></p><p>Gdy status reazerwacji zostanie zmieniony, od razu poinformujemy cię o tym w osobnej wiadomości.</p></div><p style='text-align: right;'>Pozdrawiamy <br> <i>Zespół Apartament.pl</i></p>";

            await _emailSender.SendEmailAsync(user.Email, "Dziękujemy!", body);
            return Redirect("Index");
        }

        private bool HotelExists(int id)
        {
            return _context.Hotels.Any(e => e.Id == id);
        }

        private void UploadMainPhoto(Hotel hotel, bool createMode = true)
        {
            string FolderName = _environment.ContentRootPath;
            FolderName = _environment.ContentRootPath.Remove(FolderName.Length - 2);
            string FolderPath = FolderName.Substring(0, FolderName.LastIndexOf('\\'));

            string path = Path.Combine(FolderPath, $"Hotels\\{hotel.FolderName}");

            if (createMode == true) //on creating hotel
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }

            FileInfo fileInfo = new FileInfo(hotel.HotelPhoto.FileName);
            if (!fileInfo.Extension.ToLower().Equals(".jpg") && !fileInfo.Extension.ToLower().Equals(".jpeg") && !fileInfo.Extension.ToLower().Equals(".png"))
            {
                ModelState.AddModelError("IncorrectExtensionError", "Nie prawidłowy format pliku - akceptowalne rozszerzenia to: png/jpg/jpeg");
                ViewData["FeaturesId"] = new SelectList(_context.Features, "Id", "Name");
                hotel.HotelPhoto = null;
            }
            else
            {
                string fileName = "index.jpg";
                string fileNameWithPath = Path.Combine(path, fileName);

                using (var stream = new FileStream(fileNameWithPath, FileMode.Create))
                {
                    hotel.HotelPhoto.CopyTo(stream);
                }

            }

        }

        private void UploadPhotos(Hotel hotel, bool createMode = true)
        {
            int counter = 1;
            string FolderName = _environment.ContentRootPath;
            FolderName = _environment.ContentRootPath.Remove(FolderName.Length - 2);
            string FolderPath = FolderName.Substring(0, FolderName.LastIndexOf('\\'));

            string path = Path.Combine(FolderName, $"Hotels\\{hotel.FolderName}");

            foreach (var file in hotel.HotelPhotos)
            {
                if (file.ContentType != "image/jpeg" && file.ContentType != "image/png")
                {
                    ModelState.AddModelError("IncorrectExtensionPhotoError", "Nie prawidłowy format pliku - akceptowalne rozszerzenia to: png/jpg");
                    ViewData["FeaturesId"] = new SelectList(_context.Features, "Id", "Name");
                    hotel.HotelPhotos = null;
                    break;
                }

                string tmpFileName = hotel.Name.Trim() + "-" + counter + ".jpg";
                string fileNameWithPath = Path.Combine(path, tmpFileName);
                counter++;

                using (var stream = new FileStream(fileNameWithPath, FileMode.Create))
                {
                    file.CopyTo(stream);
                }
            }
        }

        private void DeletePhotos(Hotel hotel, bool deleteMainImg = false)
        {
            string FolderName = _environment.ContentRootPath;
            FolderName = _environment.ContentRootPath.Remove(FolderName.Length - 2);
            string FolderPath = FolderName.Substring(0, FolderName.LastIndexOf('\\'));

            string path = Path.Combine(FolderPath, $"Hotels\\{hotel.FolderName}");

            foreach (String file in Directory.GetFiles(path))
            {
                FileInfo fi = new FileInfo(file);

                if (deleteMainImg == true)
                {
                    if (fi.Name == "index.jpg")
                    {
                        fi.Delete();
                    }
                }
                else
                {
                    if (fi.Name != "index.jpg")
                    {
                        fi.Delete();
                    }
                }
            }
        }

        [HttpGet]
        public async Task<IActionResult> FindHotel(string term)
        {
            if (!string.IsNullOrEmpty(term))
            {
                var hotels = await _context.Hotels.ToListAsync();
                var data = hotels
                    .Where(h => h.Name.Contains(term, StringComparison.OrdinalIgnoreCase) || h.Country.Contains(term, StringComparison.OrdinalIgnoreCase) || h.Region.Contains(term, StringComparison.OrdinalIgnoreCase)
                    || h.City.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList().AsReadOnly();
                return Ok(data);
            }
            else
            {
                return Ok();
            }
        }
    }
}
