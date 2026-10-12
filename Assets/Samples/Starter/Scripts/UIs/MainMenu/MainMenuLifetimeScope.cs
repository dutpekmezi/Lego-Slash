using GameLift.UI.IapShop;
using GameLift.UI.LevelPath;
using GameLift.UI.MainMenu.NavigationBar;
using GameLift.UI.SettingsPopup;
using TrashRush.Game;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace GameLift.UI.MainMenu
{
    public class MainMenuLifetimeScope : LifetimeScope
    {
        [SerializeField] private WeaponDatas _weaponDatas;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterInstance(_weaponDatas);
            builder.Register<WeaponSystem>(Lifetime.Singleton);

            builder.RegisterComponentInHierarchy<NavigationBarController>();
            builder.RegisterComponentInHierarchy<IapShopUI>();

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            builder.RegisterComponentInHierarchy<Cheat.CheatPanel>();
#endif

            builder.RegisterComponentInHierarchy<Currency.CurrencyBar>();
            builder.RegisterComponentInHierarchy<SettingsButton>();
            builder.RegisterComponentInHierarchy<HomeUI>();
            builder.RegisterComponentInHierarchy<PlayButton>();
            builder.RegisterComponentInHierarchy<Weapons>();
            builder.RegisterComponentInHierarchy<WeaponRenderController>();
        }
    }
}
