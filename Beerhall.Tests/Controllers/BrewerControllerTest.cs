using Beerhall.Controllers;
using Beerhall.Models.Domain;
using Beerhall.Models.ViewModels;
using Beerhall.Tests.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Beerhall.Tests.Controllers {
    public class BrewerControllerTest {
        private readonly BrewerController _controller;
        private readonly Mock<IBrewerRepository> _brewerRepository;
        private readonly Mock<ILocationRepository> _locationRepository;
        private readonly DummyApplicationDbContext _dummyContext;

        public BrewerControllerTest() {
            _dummyContext = new DummyApplicationDbContext();
            _brewerRepository = new Mock<IBrewerRepository>();
            _locationRepository = new Mock<ILocationRepository>();
            _controller = new BrewerController(_brewerRepository.Object, _locationRepository.Object)
            {
                TempData = new Mock<ITempDataDictionary>().Object
            };
        }

        #region -- Index --
        [Fact]
        public void Index_PassesOrderedListOfBrewersInViewResultModelAndStoresTotalTurnoverInViewData() {
            _brewerRepository.Setup(m => m.GetAll()).Returns(_dummyContext.Brewers);
            var result = Assert.IsType<ViewResult>(_controller.Index());
            var brewersInModel = Assert.IsType<List<Brewer>>(result.Model);
            Assert.Equal(3, brewersInModel.Count);
            Assert.Equal("Bavik", brewersInModel[0].Name);
            Assert.Equal("De Leeuw", brewersInModel[1].Name);
            Assert.Equal("Duvel Moortgat", brewersInModel[2].Name);
            Assert.Equal(20050000, result.ViewData["TotalTurnover"]);
        }
        #endregion

        #region -- Edit GET --
        [Fact]
        public void Edit_PassesBrewerInEditViewModelAndReturnsSelectListOflocations() {
            _brewerRepository.Setup(m => m.GetBy(1)).Returns(_dummyContext.Bavik);
            _locationRepository.Setup(m => m.GetAll()).Returns(_dummyContext.Locations);
            var result = Assert.IsType<ViewResult>(_controller.Edit(1));
            var brewerEvm = Assert.IsType<BrewerEditViewModel>(result.Model);
            var locationsInViewData = Assert.IsType<SelectList>(result.ViewData["Locations"]);
            Assert.Equal("Bavik", brewerEvm.Name);
            Assert.Equal("8531", brewerEvm.PostalCode);
            Assert.Equal(3, locationsInViewData.Count());
        }

        [Fact]
        public void Edit_UnknownBrewer_ReturnsNotFound() {
            _brewerRepository.Setup(m => m.GetBy(1)).Returns((Brewer)null);
            IActionResult action = _controller.Edit(1);
            Assert.IsType<NotFoundResult>(action);
        }

        #endregion

        #region -- Edit POST --
        [Fact]
        public void Edit_ValidEdit_UpdatesAndPersistsBrewerAndRedirectsToActionIndex() {
            _brewerRepository.Setup(m => m.GetBy(1)).Returns(_dummyContext.Bavik);
            var brewerEvm = new BrewerEditViewModel(_dummyContext.Bavik)
            {
                Street = "nieuwe straat 1"
            };
            var result = Assert.IsType<RedirectToActionResult>(_controller.Edit(brewerEvm, 1));
            var bavik = _dummyContext.Bavik;
            Assert.Equal("Index", result?.ActionName);
            Assert.Equal("Bavik", bavik.Name);
            Assert.Equal("nieuwe straat 1", bavik.Street);
            _brewerRepository.Verify(m => m.SaveChanges(), Times.Once());
        }

        [Fact]
        public void Edit_DomainErrors_DoesNotChangeNorPersistsBrewerAndRedirectsToActionIndex() {
            _brewerRepository.Setup(m => m.GetBy(1)).Returns(_dummyContext.Bavik);
            var brewerEvm = new BrewerEditViewModel(_dummyContext.Bavik) { Turnover = -1 };
            var result = Assert.IsType<RedirectToActionResult>(_controller.Edit(brewerEvm, 1));
            var bavik = _dummyContext.Bavik;
            Assert.Equal("Index", result.ActionName);
            Assert.Equal("Bavik", bavik.Name);
            Assert.Equal("Rijksweg 33", bavik.Street);
            Assert.Equal(20000000, bavik.Turnover);
            _brewerRepository.Verify(m => m.SaveChanges(), Times.Never());
        }

        [Fact]
        public void Edit_ModelStateErrors_DoesNotChangeNorPersistsBrewerAndPassesViewModelAndViewDataToEditView() {
            var bavik = _dummyContext.Bavik;
            _brewerRepository.Setup(m => m.GetBy(1)).Returns(bavik);
            _locationRepository.Setup(m => m.GetAll()).Returns(_dummyContext.Locations);
            BrewerEditViewModel brewerEvm = new BrewerEditViewModel(bavik)
            {
                Name = "New name"
            };
            _controller.ModelState.AddModelError("", "Error message");
            var result = Assert.IsType<ViewResult>(_controller.Edit(brewerEvm, 1));
            Assert.Equal("Edit", result.ViewName);
            Assert.Equal(brewerEvm, result.Model);
            var locations = Assert.IsType<SelectList>(result.ViewData["Locations"]);
            Assert.Equal(3, locations.Count());
            var isEdit = Assert.IsType<bool>(result.ViewData["IsEdit"]);
            Assert.True(isEdit);
            Assert.Equal("Bavik", bavik.Name);
            _brewerRepository.Verify(m => m.SaveChanges(), Times.Never());
        }

        #endregion

        #region -- Create GET --
        [Fact]
        public void Create_PassesNewBrewerInEditViewModelAndReturnsSelectListOfGemeentenWithNoSelectedValue() {
            _locationRepository.Setup(m => m.GetAll()).Returns(_dummyContext.Locations);
            var result = Assert.IsType<ViewResult>(_controller.Create());
            var locationsInViewData = Assert.IsType<SelectList>(result.ViewData["Locations"]);
            var brewerEvm = Assert.IsType<BrewerEditViewModel>(result.Model);
            Assert.Null(brewerEvm.Name);
            Assert.Equal(3, locationsInViewData.Count());
            Assert.Null(locationsInViewData?.SelectedValue);
        }

        #endregion

        #region -- Create POST --
        [Fact]
        public void Create_ValidBrewer_CreatesAndPersistsBrewerAndRedirectsToActionIndex() {
            _brewerRepository.Setup(m => m.Add(It.IsAny<Brewer>()));
            var brewerEvm = new BrewerEditViewModel(new Brewer("Chimay")
            {
                Location = _dummyContext.Locations.Last(),
                Street = "TestStraat 10 ",
                Turnover = 8000000
            });
            var result = Assert.IsType<RedirectToActionResult>(_controller.Create(brewerEvm));
            Assert.Equal("Index", result?.ActionName);
            _brewerRepository.Verify(m => m.Add(It.IsAny<Brewer>()), Times.Once());
            _brewerRepository.Verify(m => m.SaveChanges(), Times.Once());
        }

        [Fact]
        public void Create_DomainErrors_DoesNotCreateNorPersistsBrewerAndRedirectsToActionIndex() {
            _brewerRepository.Setup(m => m.Add(It.IsAny<Brewer>()));
            var brewerEvm = new BrewerEditViewModel(new Brewer("Chimay")) { Turnover = -1 };
            var result = Assert.IsType<RedirectToActionResult>(_controller.Create(brewerEvm)); ;
            Assert.Equal("Index", result.ActionName);
            _brewerRepository.Verify(m => m.Add(It.IsAny<Brewer>()), Times.Never());
            _brewerRepository.Verify(m => m.SaveChanges(), Times.Never());
        }

        [Fact]
        public void Create_ModelStateErrors_DoesNotCreateNorPersistsBrewerAndPassesViewModelAndViewDataToEditView() {
            _locationRepository.Setup(m => m.GetAll()).Returns(_dummyContext.Locations);
            _brewerRepository.Setup(m => m.GetBy(1)).Returns(_dummyContext.Bavik);
            BrewerEditViewModel brewerEvm = new BrewerEditViewModel(_dummyContext.Bavik);
            _controller.ModelState.AddModelError("", "Error message");
            var result = Assert.IsType<ViewResult>(_controller.Create(brewerEvm));
            Assert.Equal("Edit", result.ViewName);
            Assert.Equal(brewerEvm, result.Model);
            var locations = Assert.IsType<SelectList>(result.ViewData["Locations"]);
            Assert.Equal(3, locations.Count());
            var isEdit = Assert.IsType<bool>(result.ViewData["IsEdit"]);
            Assert.False(isEdit);
            _brewerRepository.Verify(m => m.Add(It.IsAny<Brewer>()), Times.Never());
            _brewerRepository.Verify(m => m.SaveChanges(), Times.Never());
        }

        [Fact]
        public void Create_ModelStateErrors_DoesNotCreateNorPersistsBrewer() {
            _brewerRepository.Setup(m => m.GetBy(1)).Returns(_dummyContext.Bavik);
            BrewerEditViewModel newBrewerEvm = new BrewerEditViewModel();
            _controller.ModelState.AddModelError("", "Error message");
            ViewResult result = _controller.Create(newBrewerEvm) as ViewResult;
            _brewerRepository.Verify(m => m.Add(It.IsAny<Brewer>()), Times.Never());
            _brewerRepository.Verify(m => m.SaveChanges(), Times.Never());
        }

        #endregion

        #region -- Delete GET --
        [Fact]
        public void Delete_PassesNameOfBrewerInViewData() {
            _brewerRepository.Setup(m => m.GetBy(1)).Returns(_dummyContext.Bavik);
            _brewerRepository.Setup(m => m.Delete(It.IsAny<Brewer>()));
            var result = Assert.IsType<ViewResult>(_controller.Delete(1));
            Assert.Equal("Bavik", result.ViewData["name"]);
        }

        [Fact]
        public void Delete_UnknownBrewer_ReturnsNotFound() {
            _brewerRepository.Setup(m => m.GetBy(1)).Returns((Brewer)null);
            IActionResult action = _controller.Delete(1);
            Assert.IsType<NotFoundResult>(action);
        }

        #endregion

        #region -- Delete POST --
        [Fact]
        public void Delete_ExistingBrewer_DeletesBrewerAndPersistsChangesAndRedirectsToActionIndex() {
            _brewerRepository.Setup(m => m.GetBy(1)).Returns(_dummyContext.Bavik);
            _brewerRepository.Setup(m => m.Delete(It.IsAny<Brewer>()));
            var result = Assert.IsType<RedirectToActionResult>(_controller.DeleteConfirmed(1));
            Assert.Equal("Index", result.ActionName);
            _brewerRepository.Verify(m => m.GetBy(1), Times.Once());
            _brewerRepository.Verify(m => m.Delete(It.IsAny<Brewer>()), Times.Once());
            _brewerRepository.Verify(m => m.SaveChanges(), Times.Once());
        }
        #endregion
    }
}
