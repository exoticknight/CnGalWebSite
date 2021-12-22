﻿using BootstrapBlazor.Components;
using CnGalWebSite.DataModel.Model;
using CnGalWebSite.DataModel.ViewModel.Admin;
using System.Threading.Tasks;

namespace CnGalWebSite.APIServer.Application.News
{
    public interface INewsService
    {
        Task UpdateNewestGameNews();

        Task UpdateWeiboUserInforCache();

        Task<WeeklyNews> GenerateNewestWeeklyNews();

        Task PublishNews(GameNews gameNews);

        Task PublishWeeklyNews(WeeklyNews weeklyNews);

        WeeklyNews ResetWeeklyNews(WeeklyNews weeklyNews);

        Task AddWeiboUserInfor(string entryName, long weiboId);

        string GenerateRealWeeklyNewsMainPage(WeeklyNews weeklyNews);

        Task<QueryData<ListGameNewAloneModel>> GetPaginatedResult(QueryPageOptions options, ListGameNewAloneModel searchModel);

        Task<QueryData<ListWeeklyNewAloneModel>> GetPaginatedResult(QueryPageOptions options, ListWeeklyNewAloneModel searchModel);
    }
}
