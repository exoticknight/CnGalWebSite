﻿using CnGalWebSite.APIServer.Application.Search.ElasticSearches;
using CnGalWebSite.DataModel.Application.Dtos;
using CnGalWebSite.DataModel.ViewModel.Home;
using System;
using System.Threading.Tasks;

namespace CnGalWebSite.APIServer.Application.Search
{
    public interface ISearchHelper
    {
        Task UpdateDataToSearchService(DateTime LastUpdateTime);

        Task DeleteDataOfSearchService();

        Task<PagedResultDto<SearchAloneModel>> QueryAsync(int page, int limit, string text, string screeningConditions, string sort, QueryType type);
    }


    public enum QueryType
    {
        /// <summary>
        /// 分页
        /// </summary>
        Page,
        /// <summary>
        /// 无限下滑
        /// </summary>
        Index
    }
}
