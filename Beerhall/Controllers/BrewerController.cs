using Beerhall.Models.Domain;
using Beerhall.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Beerhall.Controllers {
    [Authorize(Policy = "AdminOnly")]
    public class BrewerController : Controller {
        private readonly IBrewerRepository _brewerRepository;
        private readonly ILocationRepository _locationRepository;

        public BrewerController(IBrewerRepository brewerRepository, ILocationRepository locationRepository) {
            _brewerRepository = brewerRepository;
            _locationRepository = locationRepository;
        }

        [AllowAnonymous]
        public IActionResult Index() {
            IEnumerable<Brewer> brewers = _brewerRepository.GetAll().OrderBy(b => b.Name).ToList();
            ViewData["TotalTurnover"] = brewers.Sum(b => b.Turnover);
            return View(brewers);
        }

        public IActionResult Edit(int id) {
            Brewer brewer = _brewerRepository.GetBy(id);
            if (brewer == null)
                return NotFound();
            ViewData["IsEdit"] = true;
            ViewData["Locations"] = GetLocationsAsSelectList();
            return View(new BrewerEditViewModel(brewer));
        }

        [HttpPost]
        public IActionResult Edit(BrewerEditViewModel brewerEditViewModel, int id) {
            if (ModelState.IsValid)
            {
                Brewer brewer = null;
                try
                {
                    brewer = _brewerRepository.GetBy(id);
                    MapBrewerEditViewModelToBrewer(brewerEditViewModel, brewer);
                    _brewerRepository.SaveChanges();
                    TempData["message"] = $"You successfully updated brewer {brewer.Name}.";
                }
                catch
                {
                    TempData["error"] = $"Sorry, something went wrong, brewer {brewer?.Name} was not updated...";
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["IsEdit"] = true;
            ViewData["Locations"] = GetLocationsAsSelectList();
            return View(nameof(Edit), brewerEditViewModel);
        }

        public IActionResult Create() {
            ViewData["IsEdit"] = false;
            ViewData["Locations"] = GetLocationsAsSelectList();
            return View(nameof(Edit), new BrewerEditViewModel());
        }

        [HttpPost]
        public IActionResult Create(BrewerEditViewModel brewerEditViewModel) {
            if (ModelState.IsValid)
            {
                try
                {
                    Brewer brewer = new Brewer(brewerEditViewModel.Name);
                    MapBrewerEditViewModelToBrewer(brewerEditViewModel, brewer);
                    _brewerRepository.Add(brewer);
                    _brewerRepository.SaveChanges();
                    TempData["message"] = $"You successfully added brewer {brewer.Name}.";
                }
                catch
                {
                    TempData["error"] = "Sorry, something went wrong, the brewer was not added...";
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["IsEdit"] = false;
            ViewData["Locations"] = GetLocationsAsSelectList();
            return View(nameof(Edit), brewerEditViewModel);
        }

        public IActionResult Delete(int id) {
            Brewer brewer = _brewerRepository.GetBy(id);
            if (brewer == null)
                return NotFound();
            ViewData[nameof(Brewer.Name)] = brewer.Name;
            return View();
        }

        [HttpPost, ActionName("Delete")]
        public IActionResult DeleteConfirmed(int id) {
            Brewer brewer = null;
            try
            {
                brewer = _brewerRepository.GetBy(id);
                _brewerRepository.Delete(brewer);
                _brewerRepository.SaveChanges();
                TempData["message"] = $"You successfully deleted brewer {brewer.Name}.";
            }
            catch
            {
                TempData["error"] = $"Sorry, something went wrong, brewer {brewer?.Name} was not deleted…";
            }
            return RedirectToAction(nameof(Index));
        }

        private SelectList GetLocationsAsSelectList() {
            return new SelectList(
                            _locationRepository.GetAll().OrderBy(l => l.Name),
                            nameof(Location.PostalCode),
                            nameof(Location.Name));
        }

        private void MapBrewerEditViewModelToBrewer(BrewerEditViewModel brewerEditViewModel, Brewer brewer) {
            brewer.Name = brewerEditViewModel.Name;
            brewer.Street = brewerEditViewModel.Street;
            brewer.Location = brewerEditViewModel.PostalCode == null
                ? null
                : _locationRepository.GetBy(brewerEditViewModel.PostalCode);
            brewer.ContactEmail = brewerEditViewModel.ContactEmail;
            brewer.DateEstablished = brewerEditViewModel.DateEstablished;
            brewer.Description = brewerEditViewModel.Description;
            brewer.Turnover = brewerEditViewModel.Turnover;
        }
    }
}
