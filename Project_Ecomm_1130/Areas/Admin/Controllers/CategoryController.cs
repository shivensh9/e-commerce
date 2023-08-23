using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project_Ecomm_1130.DataAccess.Repository.IRepository;
using Project_Ecomm_1130.Models;
using Project_Ecomm_1130.Uitlity;

namespace Project_Ecomm_1130.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles =SD.Role_Admin)]
    public class CategoryController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        public CategoryController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Upsert(int? id)
        {
            Category category = new Category();
            if(id== null) return View(category);//Create
            //Edit
            category = _unitOfWork.Category.Get(id.GetValueOrDefault());
            if(category== null) return NotFound();
            return View(category);
        }
        [HttpPost]
        [AutoValidateAntiforgeryToken]
        public IActionResult Upsert(Category category)
        {
            if (category == null) return NotFound();
            if(!ModelState.IsValid) return View(category);
            if(category.Id==0)
                _unitOfWork.Category.Add(category); 
            else 
                _unitOfWork.Category.Update(category);
            _unitOfWork.Save();
            return RedirectToAction(nameof(Index)); 
        }

        #region APIs
        [HttpGet]
        public IActionResult GetAll()
        {
            var categoryList=_unitOfWork.Category.GetAll();
            return Json(new {data=categoryList });
        }
        [HttpDelete]
        public IActionResult Delete(int id) { 
          var categoryInDb= _unitOfWork.Category.Get(id);
            if (categoryInDb == null)
                return Json(new {success=false,message="Something went wrong while delete data !!!" });
            _unitOfWork.Category.Remove(categoryInDb);
            _unitOfWork.Save();
            return Json(new { success = true, message = "Data deleted successfully !!!" });
        }
        #endregion
    }
}
