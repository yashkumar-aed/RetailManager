﻿using RMDataManager.Library.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMDataManager.Library.DataAccess
{
    public class SaleData
    {

        public void SaveSale(SaleModel saleInfo, string cashierId)
        {
            List<SaleDetailDBModel> details = new List<SaleDetailDBModel>();
            ProductData products = new ProductData();
            var taxRate = ConfigHelper.GetTaxtRate()/100;

            foreach (var item in saleInfo.SaleDetails)
            {
                var detail = new SaleDetailDBModel
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                };
                var productInfo = products.GetProductById(item.ProductId);

                if(productInfo== null)
                {
                    throw new Exception($"{item.ProductId} brak w bazie");
                }

                detail.PurchasePrice = productInfo.RetailPrice * detail.Quantity;

                if (productInfo.IsTaxable)
                {
                    detail.Tax = detail.PurchasePrice * taxRate;
                }


                details.Add(detail);
            }

            SAleDBModel sale = new SAleDBModel
            {
                SubTotal = details.Sum(x => x.PurchasePrice),
                Tax = details.Sum(x => x.Tax),
                CashierId = cashierId
            };

            sale.Total = sale.SubTotal + sale.Tax;



            using (SqlDataAccess sql = new SqlDataAccess())
            {
                try
                {
                    sql.StartTransaction("RMData");

                    sql.SavedData<SAleDBModel>("dbo.sqlSale_Insert", sale, "RMData");

                    sale.Id = sql.LoadDataInTransaction<int, dynamic>("sqlSale_Lookup", new { sale.CashierId, sale.SaleDate }).FirstOrDefault();

                    foreach (var item in details)
                    {
                        item.SaleId = sale.Id;
                        sql.SavedDataInTransaction("dbo.sqlSaleDetail_Insert", item);
                    }

                    sql.ComitTransaction();

                }
                catch 
                {

                    sql.RollBackTransaction();
                    throw;
                }
            }

                

             


        }

        //public List<ProductModel> GetProducts()
        //{
        //    SqlDataAccess sql = new SqlDataAccess();


        //    var output = sql.LoadData<ProductModel, dynamic>("dbo.sqlProduct_GetAll", new { }, "RMData");

        //    return output;
        
    }
}
