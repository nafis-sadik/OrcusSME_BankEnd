﻿using DataLayer;
using DataLayer.Models;
using Entities;
using Microsoft.EntityFrameworkCore;
using Repositories;
using Services.Abstraction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Implementation
{
    public class CategorService : ICategoryService
    {
        private readonly ICategoryRepo _categoryRepo;
        //private readonly IProductRepo _productRepo;
        private readonly ICrashLogRepo _crashLogRepo;
        public CategorService()
        {
            OrcusUMSContext context = new OrcusUMSContext(new DbContextOptions<OrcusUMSContext>());
            _categoryRepo = new CategoryRepo(context);
            _crashLogRepo = new CrashLogRepo(context);
            //_productRepo = new IProductRepo(context);
        }

        public List<DataLayer.Models.CategoryModel> AddCategory(DataLayer.Models.CategoryModel category)
        {
            try
            {
                int pk = _categoryRepo.AsQueryable().Count() + 1;
                List<DataLayer.Models.CategoryModel> AlreadyExists = _categoryRepo.AsQueryable().Where(x => x.OutletId == category.outletId && x.CategoryName == category.CategoryName) != null ? new List<DataLayer.Models.CategoryModel>() : null;
                if (AlreadyExists != null && AlreadyExists.Count > 0)
                    return AlreadyExists;
                _categoryRepo.Add(new Entities.Category
                {
                    CategoryId = pk,
                    CategoryName = category.CategoryName,
                    OutletId = category.outletId,
                    ParentCategoryId = category.ParentCategoryId
                });

                return GetCategoriesByOutlets((int)category.outletId);
            }
            catch (Exception ex)
            {
                _categoryRepo.Rollback();

                int pk = _crashLogRepo.AsQueryable().Count() + 1;

                _crashLogRepo.Add(new CrashLog
                {
                    CrashLogId = pk,
                    ClassName = "CategorService",
                    MethodName = "AddCategory",
                    ErrorMessage = ex.Message.ToString(),
                    ErrorInner = (string.IsNullOrEmpty(ex.Message) || ex.Message == CommonConstants.MsgInInnerException ? ex.InnerException.Message : ex.Message).ToString(),
                    Data = category.UserId.ToString(),
                    TimeStamp = DateTime.Now
                });

                return null;
            }
        }

        public bool DeleteCategory(int OutletId)
        {
            try
            {
                Entities.Category category = _categoryRepo.Get(OutletId);
                // Must handle products under this category
                _categoryRepo.Delete(category);

                return true;
            }
            catch (Exception ex)
            {
                _categoryRepo.Rollback();

                int pk = _crashLogRepo.AsQueryable().Count() + 1;

                _crashLogRepo.Add(new CrashLog
                {
                    CrashLogId = pk,
                    ClassName = "CategorService",
                    MethodName = "DeleteCategory",
                    ErrorMessage = ex.Message.ToString(),
                    ErrorInner = (string.IsNullOrEmpty(ex.Message) || ex.Message == CommonConstants.MsgInInnerException ? ex.InnerException.Message : ex.Message).ToString(),
                    Data = OutletId.ToString(),
                    TimeStamp = DateTime.Now
                });

                return false;
            }
        }

        public List<DataLayer.Models.CategoryModel> GetCategoriesByOutlets(int OutletId)
        {
            try
            {
                List<DataLayer.Models.CategoryModel> response = new List<DataLayer.Models.CategoryModel>();
                response = _categoryRepo.AsQueryable().Where(x => x.OutletId == OutletId)
                    .Select(x => new DataLayer.Models.CategoryModel
                    {
                        CategoryId = x.CategoryId,
                        CategoryName = x.CategoryName,
                        outletId = (int)x.OutletId,
                        ParentCategoryId = x.ParentCategoryId,
                        UserId = x.Outlet.UserId
                    }).ToList();

                return response;
            }
            catch (Exception ex)
            {
                _categoryRepo.Rollback();

                int pk = _crashLogRepo.AsQueryable().Count() + 1;

                _crashLogRepo.Add(new CrashLog
                {
                    CrashLogId = pk,
                    ClassName = "CategorService",
                    MethodName = "GetCategoriesByOutlets",
                    ErrorMessage = ex.Message.ToString(),
                    ErrorInner = (string.IsNullOrEmpty(ex.Message) || ex.Message == CommonConstants.MsgInInnerException ? ex.InnerException.Message : ex.Message).ToString(),
                    Data = "",
                    TimeStamp = DateTime.Now
                });

                return null;
            }
        }

        private void Nestable2StringToHierarchy(string RawData, int? parentNode = null, int? outletId = null)
        {
            int bracesCounter = 0;
            List<int> CollectionId = new List<int>();

            int startIndex = 0, endIndex = 0;
            List<string> objectList = new List<string>();
            for (int i = 0; i < RawData.Length; i++)
            {
                if (RawData[i] == '{')
                {
                    // Incrementing brace cout
                    bracesCounter++;
                    // Caching start index for getting substring
                    if (bracesCounter == 1)
                        startIndex = i;
                }
                else if (RawData[i] == '}')
                {
                    // Decrementing brace cout
                    bracesCounter--;
                    // When brace count is 0, we have found an unit from same level
                    if (bracesCounter == 0)
                    {
                        // Caching end index for getting substring
                        endIndex = i;
                        // Cutting substring from start index to end index
                        objectList.Add(RawData.Substring(startIndex, (endIndex - startIndex) + 1));
                    }
                }
            }
            // Order by length so that we deal with recursion at the end of the road when we have already dealt with the rest of  the ids 
            objectList = objectList.OrderBy(x => x.Length).ToList();
            foreach (string RawObject in objectList)
            {
                int categoryId = 0;
                if (RawObject.Contains("children"))
                {
                    // Find the first occurance of c
                    int elementBreakIndex = 0;
                    for (int i = 0; i < RawObject.Length; i++)
                    {
                        if (RawObject[i] == 'c')
                        {
                            elementBreakIndex = i;
                            break;
                        }
                    }

                    // Cut from index 0 till first occurance of 'c'
                    // Split it by ':' to get substring "id" and the id number seperated.
                    // The number will always come at the second index of the array due to the format of the string
                    // The result might have a ',' as a garbage. Simply replace that with empty space and trim to get the exact number 
                    string substrContainingId = RawObject.Substring(0, elementBreakIndex);
                    substrContainingId = substrContainingId.Split(':')[1];
                    substrContainingId = substrContainingId.Replace(',', ' ');
                    substrContainingId = substrContainingId.Replace('\n', ' ');
                    substrContainingId = substrContainingId.Replace('\"', ' ');
                    substrContainingId = substrContainingId.Replace('"', ' ');
                    categoryId = int.Parse(substrContainingId.Trim());
                    var category = _categoryRepo.Get(categoryId);
                    if (category.OutletId == outletId)
                    {
                        category.ParentCategoryId = parentNode;
                    }
                    _categoryRepo.Update(category);
                    Nestable2StringToHierarchy(RawObject.Substring(1, RawObject.Length - 1), categoryId, outletId);
                }
                else
                {
                    categoryId = int.Parse(RawObject.Remove(RawObject.Length - 1, 1).Remove(0, 1).Split(':')[1]);
                    var category = _categoryRepo.Get(categoryId);
                    if (category.OutletId == outletId)
                    {
                        category.ParentCategoryId = parentNode;
                    }
                    _categoryRepo.Update(category);
                }
            }
        }
        public bool SaveHierarchy(CategoryModel categoryModel)
        {
            try
            {
                Nestable2StringToHierarchy(categoryModel.CategoryHiararchy, null, categoryModel.outletId);
                return true;
            }
            catch (Exception ex)
            {
                _categoryRepo.Rollback();

                int pk = _crashLogRepo.AsQueryable().Count() + 1;

                _crashLogRepo.Add(new CrashLog
                {
                    CrashLogId = pk,
                    ClassName = "CategorService",
                    MethodName = "GetCategoriesByOutlets",
                    ErrorMessage = ex.Message.ToString(),
                    ErrorInner = (string.IsNullOrEmpty(ex.Message) || ex.Message == CommonConstants.MsgInInnerException ? ex.InnerException.Message : ex.Message).ToString(),
                    Data = "",
                    TimeStamp = DateTime.Now
                });

                return false;
            }
        }
    }
}
