using SQLite;
using System;
using System.Collections.Generic;
using System.Text;
using Data.Model;
using System.Threading.Tasks;

namespace Data.DataAccess
{
    public class AccessLayer
    {
        readonly SQLiteAsyncConnection database;
        public AccessLayer(string dbPath)
        {
            database = new SQLiteAsyncConnection(dbPath);
            database.CreateTableAsync<BOMItem>().Wait();
            database.CreateTableAsync<DocHeader>().Wait();
            database.CreateTableAsync<DocLine>().Wait();
            database.CreateTableAsync<DBInfo>().Wait();
        }
        public Task<DBInfo> GetAuthentication()
        {
            return database.Table<DBInfo>().FirstOrDefaultAsync();
        }
        public Task<DocHeader> GetHeader(string docNum)
        {
            return database.Table<DocHeader>().Where(x=>x.DocNum==docNum).FirstOrDefaultAsync();
        }
        public Task<int> SaveAuthAsync(DBInfo info)
        {
            return database.InsertAsync(info);
        }
        public Task<int> DeleteOldInfoAsync()
        {
            return database.Table<DBInfo>().DeleteAsync();
        }
        public Task<int>Delete(DocLine d)
        {
            return database.DeleteAsync(d);
        }
        public Task<int> Delete(DocHeader h)
        {
            return database.DeleteAsync(h);
        }
        public Task<int> DeleteBOMData()
        {
            return database.ExecuteAsync("DELETE FROM BOMItem");
        }
        public Task<int> Insert(DocLine data)
        {
            return database.InsertAsync(data);
        }
        public Task<int> Insert(BOMItem data)
        {
            return database.InsertAsync(data);
        }
        public Task<int> Insert(DocHeader data)
        {
            return database.InsertAsync(data);
        }
        public Task<int> Insert(DBInfo data)
        {
            return database.InsertAsync(data);
        }
        public Task<List<DocLine>> GetLinesAsync()
        {
            return database.Table<DocLine>().ToListAsync();
        }
        public Task<List<BOMItem>> GetBOMITEMSAsync()
        {
            return database.Table<BOMItem>().ToListAsync();
        }
        public Task<List<DocLine>> GetSpecificDocsAsync(string DocNumber)
        {
            return database.Table<DocLine>().Where(i => i.DocNum == DocNumber).ToListAsync();
        }
        public Task<DocLine> GetOneSpecificDocAsync(string DocNumber)
        {
            return database.Table<DocLine>().Where(i => i.DocNum == DocNumber && i.ItemQty != 0).FirstAsync();
        }
        public Task<BOMItem> GetBOMItem(string packBarcode)
        {
            return database.Table<BOMItem>().Where(i => i.PackBarcode == packBarcode).FirstAsync();
        }
        //public Task<User> GetOneUserAsync(string guid)
        //{
        //    return database.Table<User>().Where(i => i.GUID == guid).FirstOrDefaultAsync();
        //}
        //public Task<PO> GetOnePOAsync(string LindID)
        //{
        //    return database.Table<PO>().Where(i => i.LindID == LindID).FirstOrDefaultAsync();
        //}

        //public Task<List<PO>> GetPOAllAsync()
        //{
        //    return database.Table<PO>().ToListAsync();
        //}
        //public Task<User> GetUserAsync()
        //{
        //    return database.Table<User>().FirstOrDefaultAsync();
        //}

        //public Task<List<PO>> GetPOAsync()
        //{
        //    return database.Table<PO>().ToListAsync();
        //}

        //public Task<int> SaveUserAsync(User user)
        //{
        //    return database.InsertAsync(user);
        //}
        //public Task<int> SavePOAsync(PO po)
        //{
        //    return database.InsertAsync(po);
        //}

        //public Task<int> DeleteUserAsync(User user)
        //{
        //    return database.DeleteAsync(user);
        //}
        //public Task<int> DeleteAllUsersAsync()
        //{
        //    return database.DeleteAllAsync<User>();
        //}

        //public Task<int> UpdateApproved(PO po)
        //{
        //    return database.UpdateAsync(po);
        //}
        //public Task<int> DeleteAllPOAsync()
        //{
        //    return database.DeleteAllAsync<PO>();
        //}
        //public Task<int> DeletePOAsync(PO deleter)
        //{
        //    return database.DeleteAsync(deleter);
        //}
    }
}
