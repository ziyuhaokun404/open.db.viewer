using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace Open.Db.Viewer.ShellHost.Services;

public sealed class PageViewFactory(
    IServiceProvider serviceProvider,
    IReadOnlyDictionary<Type, Type> viewTypes) : IPageViewFactory
{
    public FrameworkElement CreateView(object viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        var viewType = ResolveViewType(viewModel.GetType())
            ?? throw new InvalidOperationException($"No page view is registered for {viewModel.GetType().FullName}.");

        if (serviceProvider.GetRequiredService(viewType) is not FrameworkElement view)
        {
            throw new InvalidOperationException($"{viewType.FullName} must derive from {nameof(FrameworkElement)}.");
        }

        view.DataContext = viewModel;
        return view;
    }

    private Type? ResolveViewType(Type viewModelType)
    {
        if (viewTypes.TryGetValue(viewModelType, out var exactViewType))
        {
            return exactViewType;
        }

        return viewTypes.FirstOrDefault(entry => entry.Key.IsAssignableFrom(viewModelType)).Value;
    }
}
