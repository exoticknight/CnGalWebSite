﻿using CnGalWebSite.APIServer.Application.Helper;
using CnGalWebSite.APIServer.Application.Search;
using CnGalWebSite.APIServer.Application.Search.Typesense;
using CnGalWebSite.APIServer.CustomMiddlewares;
using CnGalWebSite.APIServer.DataReositories;
using CnGalWebSite.APIServer.Model;
using CnGalWebSite.DataModel.Application.Dtos;
using CnGalWebSite.DataModel.Application.Search.Dtos;
using CnGalWebSite.DataModel.Model;
using CnGalWebSite.DataModel.ViewModel.Search;
using CnGalWebSite.Helper.Extensions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Text;
using System.Threading.Tasks;
using Typesense;

namespace CnGalWebSite.APIServer.Application.Typesense
{
    public class TypesenseHelper : ISearchHelper
    {
        private readonly ITypesenseClient _typesenseClient;
        private readonly IRepository<Entry, int> _entryRepository;
        private readonly IRepository<Tag, int> _tagRepository;
        private readonly IRepository<Article, long> _articleRepository;
        private readonly IRepository<Periphery, long> _peripheryRepository;
        private readonly IRepository<SearchCache, long> _searchCacheRepository;

        private readonly IAppHelper _appHelper;
        private readonly string _collectionName = "SearchCache";

        public TypesenseHelper(ITypesenseProvider typesenseProvider, IRepository<Entry, int> entryRepository, IRepository<SearchCache, long> searchCacheRepository,
        IRepository<Tag, int> tagRepository, IRepository<Article, long> articleRepository, IRepository<Periphery, long> peripheryRepository, IAppHelper appHelper)
        {
            _typesenseClient = typesenseProvider.GetClient();
            _entryRepository = entryRepository;

            _tagRepository = tagRepository;
            _peripheryRepository = peripheryRepository;
            _articleRepository = articleRepository;
            _appHelper = appHelper;
            _searchCacheRepository = searchCacheRepository;
        }

        public async Task UpdateDataToSearchService(DateTime LastUpdateTime, bool updateAll = false)
        {
            await CreateCollection();
            await UpdateEntries(LastUpdateTime, updateAll);
            await UpdateArticles(LastUpdateTime, updateAll);
            await UpdatePeripheries(LastUpdateTime, updateAll);
            await UpdateTags(LastUpdateTime, updateAll);
        }

        private async Task CreateCollection()
        {
            var schema = new MySchema(
              _collectionName,
              new List<MyField>
              {
                    new MyField("id", FieldType.String, false),
                    new MyField("originalId", FieldType.Int64, false),
                    new MyField("name", FieldType.String, false, true),
                    new MyField("displayName", FieldType.String, false, true),
                    new MyField("anotherName", FieldType.String, false, true),
                    new MyField("briefIntroduction", FieldType.String, false),
                    new MyField("mainPage", FieldType.String, false),
                    new MyField("lastEditTime", FieldType.Int64, false),
                    new MyField("pubulishTime", FieldType.Int64, false),
                    new MyField("createTime", FieldType.Int64, false),
                    new MyField("readerCount", FieldType.Int32, false),
                    new MyField("type", FieldType.Int32, false),
                    new MyField("originalType", FieldType.Int32, false),
              });
            try
            {
                var createCollectionResponse = await _typesenseClient.CreateCollection(schema);
            }
            catch (Exception)
            {

            }
        }

        private async Task UpdateEntries(DateTime LastUpdateTime, bool updateAll = false)
        {
            var entries = await _entryRepository.GetAll().AsNoTracking()
                .Where(s => (s.LastEditTime > LastUpdateTime || updateAll) && s.IsHidden == false && string.IsNullOrWhiteSpace(s.Name) == false).ToListAsync();
            var documents = new List<SearchCache>();
            if (entries.Any())
            {
                var entryIds = entries.Select(s => (long)s.Id).ToList();

                documents = await _searchCacheRepository.GetAll().Where(s => s.Type == 0 && entryIds.Contains(s.OriginalId)).ToListAsync();

                foreach (var item in entries)
                {
                    var temp = documents.FirstOrDefault(s => s.OriginalId == item.Id);
                    if (temp == null)
                    {
                        temp = new SearchCache();
                        temp.Copy(item);

                        temp = await _searchCacheRepository.InsertAsync(temp);

                        temp.Copy(item);
                        temp = await _searchCacheRepository.UpdateAsync(temp);

                        documents.Add(temp);
                    }
                    else
                    {
                        temp.Copy(item);
                        temp = await _searchCacheRepository.UpdateAsync(temp);
                    }

                }

                var result = await _typesenseClient.ImportDocuments(_collectionName, documents, documents.Count, ImportType.Upsert);

                var errors = result.Where(s => s.Success == false);
                foreach (var item in errors)
                {
                    Console.WriteLine(item.Error);
                }
            }


            var deleted = await _entryRepository.GetAll().Where(s => s.IsHidden || string.IsNullOrWhiteSpace(s.Name)).Select(s => (long)s.Id).ToListAsync();
            documents = await _searchCacheRepository.GetAll().Where(s => s.Type == 0 && deleted.Contains(s.OriginalId)).ToListAsync();
            foreach (var item in documents)
            {
                try
                {
                    await _typesenseClient.DeleteDocument<SearchCache>(_collectionName, item.Id);
                }
                catch
                {

                }
            }
            if (documents.Count != 0)
            {
                await _searchCacheRepository.DeleteRangeAsync(s => s.Type == 0 && deleted.Contains(s.OriginalId));

            }
        }

        private async Task UpdateArticles(DateTime LastUpdateTime, bool updateAll = false)
        {
            var entries = await _articleRepository.GetAll().AsNoTracking()
                .Where(s => (s.LastEditTime > LastUpdateTime || updateAll) && s.IsHidden == false && string.IsNullOrWhiteSpace(s.Name) == false).ToListAsync();
            var documents = new List<SearchCache>();
            if (entries.Any())
            {
                var entryIds = entries.Select(s => s.Id).ToList();

                documents = await _searchCacheRepository.GetAll().Where(s => s.Type == 1 && entryIds.Contains(s.OriginalId)).ToListAsync();

                foreach (var item in entries)
                {
                    var temp = documents.FirstOrDefault(s => s.OriginalId == item.Id);
                    if (temp == null)
                    {
                        temp = new SearchCache();
                        temp.Copy(item);

                        temp = await _searchCacheRepository.InsertAsync(temp);

                        temp.Copy(item);
                        temp = await _searchCacheRepository.UpdateAsync(temp);

                        documents.Add(temp);
                    }
                    else
                    {
                        temp.Copy(item);
                        temp = await _searchCacheRepository.UpdateAsync(temp);

                    }

                }
                var result = await _typesenseClient.ImportDocuments(_collectionName, documents, documents.Count, ImportType.Upsert);
                var errors = result.Where(s => s.Success == false);
                foreach (var item in errors)
                {
                    Console.WriteLine(item.Error);
                }
            }

            var deleted = await _articleRepository.GetAll().Where(s => s.IsHidden || string.IsNullOrWhiteSpace(s.Name)).Select(s => s.Id).ToListAsync();
            documents = await _searchCacheRepository.GetAll().Where(s => s.Type == 1 && deleted.Contains(s.OriginalId)).ToListAsync();
            foreach (var item in documents)
            {
                try
                {
                    await _typesenseClient.DeleteDocument<SearchCache>(_collectionName, item.Id);
                }
                catch
                {

                }
            }
            if (documents.Count != 0)
            {
                await _searchCacheRepository.DeleteRangeAsync(s => s.Type == 1 && deleted.Contains(s.OriginalId));

            }
        }

        private async Task UpdatePeripheries(DateTime LastUpdateTime, bool updateAll = false)
        {
            var entries = await _peripheryRepository.GetAll().AsNoTracking()
                .Where(s => (s.LastEditTime > LastUpdateTime || updateAll) && s.IsHidden == false && string.IsNullOrWhiteSpace(s.Name) == false).ToListAsync();

            var documents = new List<SearchCache>();


            if (entries.Any())
            {
                var entryIds = entries.Select(s => s.Id).ToList();

                documents = await _searchCacheRepository.GetAll().Where(s => s.Type == 2 && entryIds.Contains(s.OriginalId)).ToListAsync();

                foreach (var item in entries)
                {
                    var temp = documents.FirstOrDefault(s => s.OriginalId == item.Id);
                    if (temp == null)
                    {
                        temp = new SearchCache();
                        temp.Copy(item);

                        temp = await _searchCacheRepository.InsertAsync(temp);

                        temp.Copy(item);
                        temp = await _searchCacheRepository.UpdateAsync(temp);

                        documents.Add(temp);
                    }
                    else
                    {
                        temp.Copy(item);
                        temp = await _searchCacheRepository.UpdateAsync(temp);

                    }

                }
                var result = await _typesenseClient.ImportDocuments(_collectionName, documents, documents.Count, ImportType.Upsert);
                var errors = result.Where(s => s.Success == false);
                foreach (var item in errors)
                {
                    Console.WriteLine(item.Error);
                }
            }

            var deleted = await _peripheryRepository.GetAll().Where(s => s.IsHidden || string.IsNullOrWhiteSpace(s.Name)).Select(s => s.Id).ToListAsync();
            documents = await _searchCacheRepository.GetAll().Where(s => s.Type == 2 && deleted.Contains(s.OriginalId)).ToListAsync();
            foreach (var item in documents)
            {
                try
                {
                    await _typesenseClient.DeleteDocument<SearchCache>(_collectionName, item.Id);

                }
                catch
                {

                }
            }
            if (documents.Count != 0)
            {
                await _searchCacheRepository.DeleteRangeAsync(s => s.Type == 2 && deleted.Contains(s.OriginalId));

            }
        }

        private async Task UpdateTags(DateTime LastUpdateTime, bool updateAll = false)
        {
            var entries = await _tagRepository.GetAll().AsNoTracking()
                .Where(s => (s.LastEditTime > LastUpdateTime || updateAll) && s.IsHidden == false && string.IsNullOrWhiteSpace(s.Name) == false).ToListAsync();

            var documents = new List<SearchCache>();


            if (entries.Any())
            {
                var entryIds = entries.Select(s => (long)s.Id).ToList();

                documents = await _searchCacheRepository.GetAll().Where(s => s.Type == 3 && entryIds.Contains(s.OriginalId)).ToListAsync();
                foreach (var item in entries)
                {
                    var temp = documents.FirstOrDefault(s => s.OriginalId == item.Id);
                    if (temp == null)
                    {
                        temp = new SearchCache();
                        temp.Copy(item);

                        temp = await _searchCacheRepository.InsertAsync(temp);

                        temp.Copy(item);
                        temp = await _searchCacheRepository.UpdateAsync(temp);

                        documents.Add(temp);
                    }
                    else
                    {
                        temp.Copy(item);
                        temp = await _searchCacheRepository.UpdateAsync(temp);

                    }

                }
                var result = await _typesenseClient.ImportDocuments(_collectionName, documents, documents.Count, ImportType.Upsert);
                var errors = result.Where(s => s.Success == false);
                foreach (var item in errors)
                {
                    Console.WriteLine(item.Error);
                }
            }


            var deleted = await _tagRepository.GetAll().Where(s => s.IsHidden || string.IsNullOrWhiteSpace(s.Name)).Select(s => (long)s.Id).ToListAsync();
            documents = await _searchCacheRepository.GetAll().Where(s => s.Type == 3 && deleted.Contains(s.OriginalId)).ToListAsync();
            foreach (var item in documents)
            {
                try
                {
                    await _typesenseClient.DeleteDocument<SearchCache>(_collectionName, item.Id);

                }
                catch
                {

                }
            }
            if (documents.Count != 0)
            {
                await _searchCacheRepository.DeleteRangeAsync(s => s.Type == 3 && deleted.Contains(s.OriginalId));

            }
        }

        public async Task DeleteDataOfSearchService()
        {
            try
            {
                await _typesenseClient.DeleteCollection(_collectionName);
                //await _searchCacheRepository.DeleteRangeAsync(s => true);
            }
            catch
            {

            }
        }


        public async Task<PagedResultDto<SearchAloneModel>> QueryAsync(int page, int limit, string text, string screeningConditions, string sort, QueryType type)
        {
            var sortString = "";
            var filterString = "";
            var isAscending = false;



            if (string.IsNullOrWhiteSpace(sort) == false)
            {
                if (sort == "Default")
                {
                    if (string.IsNullOrWhiteSpace(text))
                    {
                        sortString = "lastEditTime";
                    }
                    else
                    {
                        sortString = "_text_match";
                    }
                }
                else
                {
                    var temp = sort.Split(' ');
                    var f = temp[0][0].ToString();
                    sortString = f.ToLower() + temp[0][1..^0];
                    if (temp.Length == 1)
                    {
                        isAscending = true;
                    }
                }
            }
            else
            {
                sortString = "_text_match";
            }

            sortString = sortString.Replace("id", "originalId");
            if (isAscending == false)
            {
                sortString += ":desc";
            }
            else
            {
                sortString += ":asc";
            }

            var query = new SearchParameters(text, "name,displayName,anotherName,briefIntroduction,mainPage");
            if (string.IsNullOrWhiteSpace(sortString) == false)
            {
                query.SortBy = sortString;
            }
            query.PerPage = limit.ToString();
            query.Page = page.ToString();

            filterString = screeningConditions switch
            {
                "词条" => "type:=0",
                "文章" => "type:=1",
                "周边" => "type:=2",
                "标签" => "type:=3",
                "游戏" => "type:=0 && originalType:=0",
                "角色" => "type:=0 && originalType:=1",
                "STAFF" => "type:=0 && originalType:=3",
                "制作组" => "type:=0 && originalType:=2",
                "感想" => "type:=1 && originalType:=0",
                "访谈" => "type:=1 && originalType:=2",
                "攻略" => "type:=1 && originalType:=1",
                "动态" => "type:=1 && originalType:=3",
                "评测" => "type:=1 && originalType:=4",
                "周边文章" => "type:=1 && originalType:=5",
                "公告" => "type:=1 && originalType:=6",
                "杂谈" => "type:=1 && originalType:=7",
                "二创" => "type:=1 && originalType:=8",
                "设定集或画册等" => "type:=2 && originalType:=0",
                "原声集" => "type:=2 && originalType:=1",
                "套装" => "type:=2 && originalType:=2",
                "其他周边" => "type:=2 && originalType:=3",
                _ => ""
            };

            if (string.IsNullOrWhiteSpace(filterString) == false)
            {
                query.FilterBy = filterString;
            }

            var searchResult = await _typesenseClient.Search<SearchCache>(_collectionName, query);

            var model = await ProcSearchResult(searchResult);
            model.FilterText = text;
            model.MaxResultCount = limit;
            model.FilterText = text;
            model.ScreeningConditions = screeningConditions;

            model.Sorting = sort;
            return model;
        }

        private async Task<PagedResultDto<SearchAloneModel>> ProcSearchResult(SearchResult<SearchCache> model)
        {
            //根据查询结果向数据库获取真实信息

            var entryIds = model.Hits.Where(s => s.Document.Type == 0).Select(s => s.Document.OriginalId).ToList();

            var entries = await _entryRepository.GetAll().AsNoTracking().Include(s => s.Information)
                    .Include(s => s.EntryRelationFromEntryNavigation).ThenInclude(s => s.ToEntryNavigation).ThenInclude(s => s.Information).ThenInclude(s => s.Additional)
                    .Include(s => s.EntryRelationFromEntryNavigation).ThenInclude(s => s.ToEntryNavigation).ThenInclude(s => s.EntryRelationFromEntryNavigation).ThenInclude(s => s.ToEntryNavigation)
                .Where(s => entryIds.Contains(s.Id) && s.IsHidden != true && string.IsNullOrWhiteSpace(s.Name) == false).ToListAsync();

            var articleIds = model.Hits.Where(s => s.Document.Type == 1).Select(s => s.Document.OriginalId).ToList();

            var articles = await _articleRepository.GetAll().AsNoTracking().Include(s => s.CreateUser).Where(s => articleIds.Contains(s.Id) && s.IsHidden != true && string.IsNullOrWhiteSpace(s.Name) == false).ToListAsync();

            var peripheryIds = model.Hits.Where(s => s.Document.Type == 2).Select(s => s.Document.OriginalId).ToList();

            var peripheries = await _peripheryRepository.GetAll().AsNoTracking().Include(s => s.RelatedEntries).Where(s => peripheryIds.Contains(s.Id) && s.IsHidden != true && string.IsNullOrWhiteSpace(s.Name) == false).ToListAsync();

            var tagIds = model.Hits.Where(s => s.Document.Type == 3).Select(s => s.Document.OriginalId).ToList();

            var tags = await _tagRepository.GetAll().AsNoTracking().Where(s => tagIds.Contains(s.Id) && s.IsHidden != true && string.IsNullOrWhiteSpace(s.Name) == false).ToListAsync();



            var result = new PagedResultDto<SearchAloneModel>
            {
                TotalCount = model.Found,
                CurrentPage = model.Page,
            };

            //将真实信息处理后按顺序添加到结果中
            foreach (var item in model.Hits)
            {
                if (item.Document.Type == 0)
                {
                    result.Data.Add(new SearchAloneModel
                    {
                        entry = await _appHelper.GetEntryInforTipViewModel(entries.FirstOrDefault(s => s.Id == item.Document.OriginalId))
                    });
                }
                else if (item.Document.Type == 1)
                {
                    result.Data.Add(new SearchAloneModel
                    {
                        article = _appHelper.GetArticleInforTipViewModel(articles.FirstOrDefault(s => s.Id == item.Document.OriginalId))
                    });
                }

                else if (item.Document.Type == 2)
                {
                    result.Data.Add(new SearchAloneModel
                    {
                        periphery = _appHelper.GetPeripheryInforTipViewModel(peripheries.FirstOrDefault(s => s.Id == item.Document.OriginalId))
                    });
                }
                else if (item.Document.Type == 3)
                {
                    result.Data.Add(new SearchAloneModel
                    {
                        tag = _appHelper.GetTagInforTipViewModel(tags.FirstOrDefault(s => s.Id == item.Document.OriginalId))
                    });
                }
            }


            return result;
        }

        public async Task<PagedResultDto<SearchAloneModel>> QueryAsync(SearchInputModel model)
        {
            var sortString = "";
            var filterString = new StringBuilder();
            var isAscending = !model.Sorting.Contains(" desc");
            model.Sorting = model.Sorting.Replace(" desc", "");

            //初始化排序
            if (string.IsNullOrWhiteSpace(model.Sorting) == false)
            {

                var f = model.Sorting[0].ToString();
                sortString = f.ToLower() + model.Sorting[1..^0];

            }
            else
            {
                if (string.IsNullOrWhiteSpace(model.FilterText))
                {
                    sortString = "lastEditTime";
                    isAscending = false;
                }
                else
                {
                    sortString = "_text_match";
                }
            }

            sortString = sortString.Replace("id", "originalId");
            if (isAscending == false)
            {
                sortString += ":desc";
            }
            else
            {
                sortString += ":asc";
            }

            //设置搜索字段
            var query = new SearchParameters(model.FilterText, "name,displayName,anotherName,briefIntroduction,mainPage");
            //设置排序
            if (string.IsNullOrWhiteSpace(sortString) == false)
            {
                query.SortBy = sortString;
            }

            //页数
            query.PerPage = model.MaxResultCount.ToString();
            query.Page = model.CurrentPage.ToString();

            //筛选时间
            if (model.Times.Any())
            {
                foreach (var item in model.Times)
                {
                    if (filterString.Length != 0)
                    {
                        filterString.Append(" && ");
                    }
                    filterString.Append($"pubulishTime: [{item.AfterTime.ToBinary()}..{item.BeforeTime.ToBinary()}]");
                }
            }

            //筛选类别
            if (model.Types.Any())
            {
                var types = new List<int>();

                foreach (var item in model.Types)
                {
                    if (item == SearchType.Entry || item == SearchType.Article || item == SearchType.Periphery || item == SearchType.Tag)
                    {
                        types.AddRange(item.ToTypeList());
                    }
                    else
                    {
                        types.Add((int)item);
                    }
                }

                types = types.Distinct().ToList();

                if (types.Count > 0)
                {
                    if (filterString.Length != 0)
                    {
                        filterString.Append(" && ");
                    }
                    filterString.Append($"originalType: [");
                    foreach (var item in types)
                    {
                        filterString.Append(item.ToString());

                        if (types.IndexOf(item) != types.Count - 1)
                        {
                            filterString.Append(", ");
                        }
                    }
                    filterString.Append(']');
                }
            }


            if (filterString.Length != 0)
            {
                query.FilterBy = filterString.ToString();
            }

            //进行搜索
            var searchResult = await _typesenseClient.Search<SearchCache>(_collectionName, query);

            var result = await ProcSearchResult(searchResult);

            result.MaxResultCount = model.MaxResultCount;

            return result;
        }
    }
}
