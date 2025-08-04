﻿namespace CinemaApp.Services.Core.Admin.Interfaces
{
    using Core.Interfaces;
    using Web.ViewModels.Admin.CinemaManagement;

    public interface ICinemaManagementService : ICinemaService
    {
        Task<IEnumerable<CinemaManagementIndexViewModel>> GetCinemaManagementBoardDataAsync();

        Task<bool> AddCinemaAsync(CinemaManagementAddFormModel? inputModel);

        Task<CinemaManagementEditFormModel?> GetCinemaEditFormModelAsync(string? id);

        Task<bool> EditCinemaAsync(CinemaManagementEditFormModel? inputModel);

        Task<Tuple<bool, bool>> DeleteOrRestoreCinemaAsync(string? id);
    }
}
