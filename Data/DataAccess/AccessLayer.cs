using SQLite;
using System;
using System.Collections.Generic;
using System.Text;
using Data.Model;

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
        }
        //public Task<User> GetOneUserAsync(string guid)
        //{
        //    return database.Table<User>().Where(i => i.GUID == guid).FirstOrDefaultAsync();
        //}
        //public Task<PO> GetOnePOAsync(string LindID)
        //{
        //    return database.Table<PO>().Where(i => i.LindID == LindID).FirstOrDefaultAsync();
        //}
        //public Task<List<User>> GetUsersAsync()
        //{
        //    return database.Table<User>().ToListAsync();
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
