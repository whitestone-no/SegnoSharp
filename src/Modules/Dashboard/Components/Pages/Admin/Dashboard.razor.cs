using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using Whitestone.SegnoSharp.Modules.Dashboard.Models;
using Whitestone.SegnoSharp.Modules.Dashboard.ViewModels;
using Whitestone.SegnoSharp.Shared.Interfaces;

namespace Whitestone.SegnoSharp.Modules.Dashboard.Components.Pages.Admin
{
    public partial class Dashboard
    {
        [Inject] private IEnumerable<IModule> Modules { get; set; }
        [Inject] private DashboardSettings DashboardSettings { get; set; }

        private List<DashboardBoxViewModel> _availableDashboardBoxes = [];
        private List<DashboardBoxViewModel> _activeDashboardBoxes = [];

        protected override void OnInitialized()
        {
            List<DashboardBoxViewModel> boxes = [];

            foreach (IModule module in Modules)
            {
                foreach (Type dashboardBoxType in module.GetType().Assembly.GetTypes())
                {
                    if (!dashboardBoxType.IsAssignableTo(typeof(IComponent)) ||
                        !dashboardBoxType.IsAssignableTo(typeof(IDashboardBox)))
                    {
                        continue;
                    }

                    DashboardBoxViewModel box = new()
                    {
                        Id = dashboardBoxType.FullName?.Replace(".", "-"),
                        Name = dashboardBoxType.GetProperty(nameof(IDashboardBox.Name))?.GetValue(null)?.ToString(),
                        Type = dashboardBoxType,
                        Title = dashboardBoxType.GetProperty(nameof(IDashboardBox.Title))?.GetValue(null)?.ToString(),
                        AdditionalCss = dashboardBoxType.GetProperty(nameof(IDashboardBox.AdditionalCss))?.GetValue(null)?.ToString()
                    };

                    boxes.Add(box);
                }
            }

            foreach (string boxId in DashboardSettings.DashboardBoxes.ToList())
            {
                DashboardBoxViewModel box = boxes.Find(b => b.Id == boxId);

                if (box == null)
                {
                    DashboardSettings.DashboardBoxes.Remove(boxId);
                    continue;
                }

                _activeDashboardBoxes.Add(box);
            }

            _availableDashboardBoxes = boxes
                .Where(b => _activeDashboardBoxes.All(ab => ab.Id != b.Id))
                .ToList();
        }

        private DashboardBoxViewModel _currentlyDraggingActiveBox;
        private DashboardBoxViewModel _currentlyDraggingAvailableBox;
        private DashboardBoxViewModel _currentlyDraggingOverBox;
        private bool _currentlyDraggingOverActiveLast;
        private bool _currentlyDraggingOverAvailableLast;

        private void DragAvailableStart(DashboardBoxViewModel box)
        {
            _currentlyDraggingAvailableBox = box;
        }

        private void DragActiveStart(DashboardBoxViewModel box)
        {
            _currentlyDraggingActiveBox = box;
        }

        private void HandleActivateDrop(DashboardBoxViewModel targetBox)
        {
            if (targetBox != null &&
                targetBox == _currentlyDraggingActiveBox)
            {
                return;
            }

            DashboardBoxViewModel sourceBox;
            if (_currentlyDraggingAvailableBox != null)
            {
                sourceBox = _currentlyDraggingAvailableBox;
                _availableDashboardBoxes.Remove(_currentlyDraggingAvailableBox);
            }
            else if (_currentlyDraggingActiveBox != null)
            {
                sourceBox = _currentlyDraggingActiveBox;
                _activeDashboardBoxes.Remove(_currentlyDraggingActiveBox);
            }
            else
            {
                return;
            }

            if (targetBox == null)
            {
                _activeDashboardBoxes.Add(sourceBox);
            }
            else
            {
                int targetIndex = _activeDashboardBoxes.IndexOf(targetBox);
                _activeDashboardBoxes.Insert(targetIndex, sourceBox);
            }

            DashboardSettings.DashboardBoxes = _activeDashboardBoxes
                .Select(b => b.Id)
                .ToList();
        }

        private void HandleDeactivateDrop(DashboardBoxViewModel targetBox)
        {
            DashboardBoxViewModel sourceBox;
            if (_currentlyDraggingActiveBox != null)
            {
                sourceBox = _currentlyDraggingActiveBox;
                _activeDashboardBoxes.Remove(_currentlyDraggingActiveBox);
            }
            else
            {
                return;
            }

            if (targetBox == null)
            {
                _availableDashboardBoxes.Add(sourceBox);
            }
            else
            {
                int targetIndex = _availableDashboardBoxes.IndexOf(targetBox);
                _availableDashboardBoxes.Insert(targetIndex, sourceBox);
            }

            DashboardSettings.DashboardBoxes = _activeDashboardBoxes
                .Select(b => b.Id)
                .ToList();
        }

        private void HandleDragEnd()
        {
            _currentlyDraggingActiveBox = null;
            _currentlyDraggingAvailableBox = null;
            _currentlyDraggingOverBox = null;

            _currentlyDraggingOverActiveLast = false;
            _currentlyDraggingOverAvailableLast = false;
        }

        private void HandleDragEnter(DashboardBoxViewModel box)
        {
            if (_currentlyDraggingActiveBox != null || _currentlyDraggingAvailableBox != null)
            {
                _currentlyDraggingOverBox = box;
            }

            _currentlyDraggingOverActiveLast = false;
            _currentlyDraggingOverAvailableLast = false;
        }

        private void HandleDragEnterActiveLast()
        {
            _currentlyDraggingOverBox = null;
            _currentlyDraggingOverActiveLast = true;
        }

        private void HandleDragEnterAvailableLast()
        {
            _currentlyDraggingOverBox = null;
            _currentlyDraggingOverAvailableLast = true;
        }
    }
}
