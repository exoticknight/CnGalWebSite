﻿using BootstrapBlazor.Components;
using CnGalWebSite.APIServer.DataReositories;
using CnGalWebSite.DataModel.Model;
using CnGalWebSite.DataModel.ViewModel.Robots;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace CnGalWebSite.APIServer.Application.Robots
{
    public class RobotService : IRobotService
    {

        private readonly IRepository<RobotReply, long> _robotReplyRepository;
        private readonly IRepository<RobotGroup, long> _robotGroupRepository;
        private readonly IRepository<RobotEvent, long> _robotEventRepository;
        private readonly IRepository<RobotFace, long> _robotFacesRepository;

        private static readonly ConcurrentDictionary<Type, Func<IEnumerable<RobotReply>, string, SortOrder, IEnumerable<RobotReply>>> SortLambdaCacheRobotReply = new();
        private static readonly ConcurrentDictionary<Type, Func<IEnumerable<RobotGroup>, string, SortOrder, IEnumerable<RobotGroup>>> SortLambdaCacheRobotGroup = new();
        private static readonly ConcurrentDictionary<Type, Func<IEnumerable<RobotEvent>, string, SortOrder, IEnumerable<RobotEvent>>> SortLambdaCacheRobotEvent = new();
        private static readonly ConcurrentDictionary<Type, Func<IEnumerable<RobotFace>, string, SortOrder, IEnumerable<RobotFace>>> SortLambdaCacheRobotFace = new();

        public RobotService(IRepository<RobotReply, long> robotReplyRepository, IRepository<RobotGroup, long> robotGroupRepository, IRepository<RobotEvent, long> robotEventRepository, IRepository<RobotFace, long> robotFacesRepository)
        {
            _robotEventRepository = robotEventRepository;
            _robotGroupRepository = robotGroupRepository;
            _robotReplyRepository = robotReplyRepository;
            _robotFacesRepository = robotFacesRepository;
        }

        public Task<QueryData<ListRobotReplyAloneModel>> GetPaginatedResult(CnGalWebSite.DataModel.ViewModel.Search.QueryPageOptions options, ListRobotReplyAloneModel searchModel)
        {
            IEnumerable<RobotReply> items = _robotReplyRepository.GetAll().AsNoTracking();
            // 处理高级搜索
            if (!string.IsNullOrWhiteSpace(searchModel.Key))
            {
                items = items.Where(item => item.Key?.Contains(searchModel.Key, StringComparison.OrdinalIgnoreCase) ?? false);
            }
            if (!string.IsNullOrWhiteSpace(searchModel.Value))
            {
                items = items.Where(item => item.Value?.Contains(searchModel.Value, StringComparison.OrdinalIgnoreCase) ?? false);
            }




            // 处理 SearchText 模糊搜索
            if (!string.IsNullOrWhiteSpace(options.SearchText))
            {
                items = items.Where(item => (item.Key?.Contains(options.SearchText) ?? false)
                  || (item.Value?.Contains(options.SearchText) ?? false));
            }


            // 排序
            var isSorted = false;
            if (!string.IsNullOrWhiteSpace(options.SortName))
            {
                // 外部未进行排序，内部自动进行排序处理
                var invoker = SortLambdaCacheRobotReply.GetOrAdd(typeof(RobotReply), key => LambdaExtensions.GetSortLambda<RobotReply>().Compile());
                items = invoker(items, options.SortName, (BootstrapBlazor.Components.SortOrder)options.SortOrder);
                isSorted = true;
            }

            // 设置记录总数
            var total = items.Count();

            // 内存分页
            items = items.Skip((options.PageIndex - 1) * options.PageItems).Take(options.PageItems).ToList();

            //复制数据
            var resultItems = new List<ListRobotReplyAloneModel>();
            foreach (var item in items)
            {
                resultItems.Add(new ListRobotReplyAloneModel
                {
                    Id = item.Id,
                    IsHidden = item.IsHidden,
                    Key = item.Key,
                    UpdateTime = item.UpdateTime,
                    Value = item.Value,
                    AfterTime = item.AfterTime,
                    Range = item.Range,
                    Priority = item.Priority,
                    BeforeTime = item.BeforeTime,
                });
            }

            return Task.FromResult(new QueryData<ListRobotReplyAloneModel>()
            {
                Items = resultItems,
                TotalCount = total,
                IsSorted = isSorted,
                // IsFiltered = isFiltered
            });
        }

        public Task<QueryData<ListRobotGroupAloneModel>> GetPaginatedResult(CnGalWebSite.DataModel.ViewModel.Search.QueryPageOptions options, ListRobotGroupAloneModel searchModel)
        {
            IEnumerable<RobotGroup> items = _robotGroupRepository.GetAll().AsNoTracking();
            // 处理高级搜索
            if (searchModel.GroupId > 0)
            {
                items = items.Where(item => item.GroupId.ToString()?.Contains(searchModel.GroupId.ToString(), StringComparison.OrdinalIgnoreCase) ?? false);
            }
            if (!string.IsNullOrWhiteSpace(searchModel.Note))
            {
                items = items.Where(item => item.Note?.Contains(searchModel.Note, StringComparison.OrdinalIgnoreCase) ?? false);
            }



            // 处理 SearchText 模糊搜索
            if (!string.IsNullOrWhiteSpace(options.SearchText))
            {
                items = items.Where(item => (item.GroupId.ToString()?.Contains(options.SearchText) ?? false)
                 || (item.Note?.Contains(options.SearchText) ?? false));
            }


            // 排序
            var isSorted = false;
            if (!string.IsNullOrWhiteSpace(options.SortName))
            {
                // 外部未进行排序，内部自动进行排序处理
                var invoker = SortLambdaCacheRobotGroup.GetOrAdd(typeof(RobotGroup), key => LambdaExtensions.GetSortLambda<RobotGroup>().Compile());
                items = invoker(items, options.SortName, (BootstrapBlazor.Components.SortOrder)options.SortOrder);
                isSorted = true;
            }

            // 设置记录总数
            var total = items.Count();

            // 内存分页
            items = items.Skip((options.PageIndex - 1) * options.PageItems).Take(options.PageItems).ToList();

            //复制数据
            var resultItems = new List<ListRobotGroupAloneModel>();
            foreach (var item in items)
            {
                resultItems.Add(new ListRobotGroupAloneModel
                {
                    Id = item.Id,
                    IsHidden = item.IsHidden,
                    Note = item.Note,
                    ForceMatch = item.ForceMatch,
                    GroupId = item.GroupId,
                });
            }

            return Task.FromResult(new QueryData<ListRobotGroupAloneModel>()
            {
                Items = resultItems,
                TotalCount = total,
                IsSorted = isSorted,
                // IsFiltered = isFiltered
            });
        }

        public Task<QueryData<ListRobotEventAloneModel>> GetPaginatedResult(CnGalWebSite.DataModel.ViewModel.Search.QueryPageOptions options, ListRobotEventAloneModel searchModel)
        {
            IEnumerable<RobotEvent> items = _robotEventRepository.GetAll().AsNoTracking();
            // 处理高级搜索
            if (!string.IsNullOrWhiteSpace(searchModel.Text))
            {
                items = items.Where(item => item.Text?.Contains(searchModel.Text, StringComparison.OrdinalIgnoreCase) ?? false);
            }
            if (!string.IsNullOrWhiteSpace(searchModel.Note))
            {
                items = items.Where(item => item.Note?.Contains(searchModel.Note, StringComparison.OrdinalIgnoreCase) ?? false);
            }



            // 处理 SearchText 模糊搜索
            if (!string.IsNullOrWhiteSpace(options.SearchText))
            {
                items = items.Where(item => (item.Text?.Contains(options.SearchText) ?? false)
                 || (item.Note?.Contains(options.SearchText) ?? false));
            }


            // 排序
            var isSorted = false;
            if (!string.IsNullOrWhiteSpace(options.SortName))
            {
                // 外部未进行排序，内部自动进行排序处理
                var invoker = SortLambdaCacheRobotEvent.GetOrAdd(typeof(RobotEvent), key => LambdaExtensions.GetSortLambda<RobotEvent>().Compile());
                items = invoker(items, options.SortName, (BootstrapBlazor.Components.SortOrder)options.SortOrder);
                isSorted = true;
            }

            // 设置记录总数
            var total = items.Count();

            // 内存分页
            items = items.Skip((options.PageIndex - 1) * options.PageItems).Take(options.PageItems).ToList();

            //复制数据
            var resultItems = new List<ListRobotEventAloneModel>();
            foreach (var item in items)
            {
                resultItems.Add(new ListRobotEventAloneModel
                {
                    Id = item.Id,
                    IsHidden = item.IsHidden,
                    Text = item.Text,
                    Probability = item.Probability,
                    Type = item.Type,
                    Time = item.Time,
                    Note = item.Note,
                    DelaySecond = item.DelaySecond,
                });
            }

            return Task.FromResult(new QueryData<ListRobotEventAloneModel>()
            {
                Items = resultItems,
                TotalCount = total,
                IsSorted = isSorted,
                // IsFiltered = isFiltered
            });
        }

        public Task<QueryData<ListRobotFaceAloneModel>> GetPaginatedResult(CnGalWebSite.DataModel.ViewModel.Search.QueryPageOptions options, ListRobotFaceAloneModel searchModel)
        {
            IEnumerable<RobotFace> items = _robotFacesRepository.GetAll().AsNoTracking();
            // 处理高级搜索
            if (!string.IsNullOrWhiteSpace(searchModel.Key))
            {
                items = items.Where(item => item.Key?.Contains(searchModel.Key, StringComparison.OrdinalIgnoreCase) ?? false);
            }
            if (!string.IsNullOrWhiteSpace(searchModel.Value))
            {
                items = items.Where(item => item.Value?.Contains(searchModel.Value, StringComparison.OrdinalIgnoreCase) ?? false);
            }
            if (!string.IsNullOrWhiteSpace(searchModel.Note))
            {
                items = items.Where(item => item.Note?.Contains(searchModel.Note, StringComparison.OrdinalIgnoreCase) ?? false);
            }



            // 处理 SearchText 模糊搜索
            if (!string.IsNullOrWhiteSpace(options.SearchText))
            {
                items = items.Where(item => (item.Value?.Contains(options.SearchText) ?? false)
                 || (item.Key?.Contains(options.SearchText) ?? false)
                  || (item.Note?.Contains(options.SearchText) ?? false));
            }


            // 排序
            var isSorted = false;
            if (!string.IsNullOrWhiteSpace(options.SortName))
            {
                // 外部未进行排序，内部自动进行排序处理
                var invoker = SortLambdaCacheRobotFace.GetOrAdd(typeof(RobotFace), key => LambdaExtensions.GetSortLambda<RobotFace>().Compile());
                items = invoker(items, options.SortName, (BootstrapBlazor.Components.SortOrder)options.SortOrder);
                isSorted = true;
            }

            // 设置记录总数
            var total = items.Count();

            // 内存分页
            items = items.Skip((options.PageIndex - 1) * options.PageItems).Take(options.PageItems).ToList();

            //复制数据
            var resultItems = new List<ListRobotFaceAloneModel>();
            foreach (var item in items)
            {
                resultItems.Add(new ListRobotFaceAloneModel
                {
                    Id = item.Id,
                    IsHidden = item.IsHidden,
                    Key = item.Key,
                    Value = item.Value,
                    Note = item.Note,
                });
            }

            return Task.FromResult(new QueryData<ListRobotFaceAloneModel>()
            {
                Items = resultItems,
                TotalCount = total,
                IsSorted = isSorted,
                // IsFiltered = isFiltered
            });
        }

    }
}
