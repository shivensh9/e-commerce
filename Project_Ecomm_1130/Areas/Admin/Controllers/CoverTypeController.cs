using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project_Ecomm_1130.DataAccess.Repository.IRepository;
using Project_Ecomm_1130.Models;
using Project_Ecomm_1130.Uitlity;

namespace Project_Ecomm_1130.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles =SD.Role_Admin)]
    public class CoverTypeController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        public CoverTypeController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Upsert(int? id)
        {
            CoverType coverType = new CoverType();
            if(id==null) return View(coverType);
            // coverType=_unitOfWork.CoverType.Get(id.GetValueOrDefault());
            var param = new DynamicParameters();
            param.Add("@id",id.GetValueOrDefault());
            coverType=_unitOfWork.SPCALL.OneRecord<CoverType>
                (SD.Proc_GetCoverType,param);

            if (coverType==null) return NotFound();  
            return View(coverType); 
        }
        [HttpPost]
        [AutoValidateAntiforgeryToken]
        public IActionResult Upsert(CoverType coverType)
        {
            if(coverType== null) return BadRequest();   
            if(!ModelState.IsValid) return View(coverType);
            var param = new DynamicParameters();
            param.Add("@name", coverType.Name);
            if (coverType.Id == 0)
                //  _unitOfWork.CoverType.Add(coverType);
                _unitOfWork.SPCALL.Execute(SD.Proc_CreateCoverType,param);
            else
            {
                param.Add("@id", coverType.Id);
                _unitOfWork.SPCALL.Execute(SD.Proc_UpdateCoverType,param);
            }
             //   _unitOfWork.CoverType.Update(coverType);
          //  _unitOfWork.Save();
            return RedirectToAction("Index");
        }
        #region APIs
        [HttpGet]
        public IActionResult GetAll()
        {
            // return Json(new { data=_unitOfWork.CoverType.GetAll() });
            return Json(new {data=_unitOfWork.SPCALL.
                List<CoverType>(SD.Proc_GetCoverTypes) });
        }
        [HttpDelete]
        public IActionResult Delete(int id)
        {
            var param = new DynamicParameters();
            param.Add("@id", id);
            // var coverTypeInDb=_unitOfWork.CoverType.Get(id);
            var coverTypeInDb = _unitOfWork.SPCALL.OneRecord<CoverType>
                (SD.Proc_GetCoverType,param);
            if (coverTypeInDb == null)
                return Json(new {success=false,message="Something went wrong while delete data ? !!" });
             
            _unitOfWork.SPCALL.Execute(SD.Proc_DeleteCoverType,param);
            // _unitOfWork.CoverType.Remove(coverTypeInDb);
            //_unitOfWork.Save();
            return Json(new { success = true, message = "data deleted successfully ? !!" });
        }
        #endregion
    }
}
