using System.Collections.Generic;
using GameLift.Buttons;
using GameLift.Currency;
using GameLift.Popup;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace TrashRush.Game
{
    public sealed class Weapons : PopupBase
    {
        [SerializeField] private WeaponCardUI _weaponCardPrefab;
        [SerializeField] private Transform _iapItemContainer;

        [Header("Selected Weapon Info")]
        [SerializeField] private TMP_Text _weaponNameText;
        [SerializeField] private TMP_Text _weaponDescriptionText;
        [SerializeField] private TMP_Text _rareText;
        [SerializeField] private Image _rareLabel;
        [SerializeField] private TMP_Text _costAmountText;

        [Header("Buttons")]
        [SerializeField] private Button _buyButton;
        [SerializeField] private Image _buyButtonImage;
        [SerializeField] private TMP_Text _buyButtonText;
        [SerializeField] private GameObject _costGroup;
        [SerializeField] private Button _selectButton;
        [SerializeField] private BaseButton _closeButton;

        [Header("Purchase")]
        [SerializeField] private string _purchaseCurrencyId = CurrencyIds.Gold;
        [SerializeField, Min(0)] private int _startIndex;

        private readonly List<WeaponCardUI> _cards = new();
        private ICurrencyService _currencyService;
        private IObjectResolver _resolver;
        private WeaponDatas _weaponDatas;
        private WeaponRenderController _weaponRenderController;
        private int _selectedIndex = -1;
        private bool _isInitialized;

        public override string PopupId => PopupIdConst;
        public const string PopupIdConst = "weapons";

        private IReadOnlyList<WeaponData> WeaponItems =>
            _weaponDatas != null ? _weaponDatas.Weapons : System.Array.Empty<WeaponData>();

        public WeaponData SelectedWeapon =>
            IsValidWeaponIndex(_selectedIndex) ? WeaponItems[_selectedIndex] : null;

        [Inject]
        private void Construct(
            ICurrencyService currencyService,
            IObjectResolver resolver,
            WeaponDatas weaponDatas,
            WeaponRenderController weaponRenderController)
        {
            _currencyService = currencyService;
            _resolver = resolver;
            _weaponDatas = weaponDatas;
            _weaponRenderController = weaponRenderController;

            ResolveUiReferences();
            InjectCloseButton();
            Initialize();
            RefreshSelectedWeaponState();
        }

        protected override void Awake()
        {
            base.Awake();

            ResolveUiReferences();
            RegisterButtons();
        }

        private void OnEnable()
        {
            RefreshSelectedWeaponState();
        }

        private void OnDestroy()
        {
            UnregisterButtons();
        }

        public void Show(int index)
        {
            if (WeaponItems.Count == 0)
            {
                _selectedIndex = -1;
                _weaponRenderController?.Clear();
                RefreshSelectedWeaponState();
                return;
            }

            index = WrapIndex(index);

            if (!IsValidWeaponIndex(index) &&
                !TryFindValidIndex(index, out index))
            {
                _selectedIndex = -1;
                _weaponRenderController?.Clear();
                RefreshSelectedWeaponState();
                return;
            }

            _selectedIndex = index;
            _weaponRenderController?.Show(SelectedWeapon);
            RefreshSelectedWeaponState();
        }

        public void Show(WeaponData weaponData)
        {
            if (weaponData == null)
            {
                return;
            }

            for (var i = 0; i < WeaponItems.Count; i++)
            {
                if (WeaponItems[i] == weaponData)
                {
                    Show(i);
                    return;
                }
            }
        }

        public bool BuySelectedWeapon()
        {
            var weaponData = SelectedWeapon;

            if (weaponData == null)
            {
                RefreshSelectedWeaponState();
                return false;
            }

            if (IsPurchased(weaponData))
            {
                RefreshSelectedWeaponState();
                return true;
            }

            if (weaponData.Cost > 0)
            {
                if (_currencyService == null)
                {
                    Debug.LogWarning($"{nameof(Weapons)} needs {nameof(ICurrencyService)} injection to buy weapons.", this);
                    RefreshSelectedWeaponState();
                    return false;
                }

                if (!_currencyService.TryPurchase(_purchaseCurrencyId, weaponData.Cost))
                {
                    RefreshSelectedWeaponState();
                    return false;
                }
            }

            MarkPurchased(weaponData);
            RefreshSelectedWeaponState();
            return true;
        }

        public bool SelectSelectedWeapon()
        {
            var weaponData = SelectedWeapon;

            if (weaponData == null || !IsPurchased(weaponData))
            {
                RefreshSelectedWeaponState();
                return false;
            }

            PlayerPrefs.SetString(WeaponSaveKeys.EquippedWeapon, weaponData.SaveId);
            PlayerPrefs.Save();
            RefreshSelectedWeaponState();
            return true;
        }

        public bool EquipSelectedWeapon()
        {
            return SelectSelectedWeapon();
        }

        public bool BuyAndSelectSelectedWeapon()
        {
            if (!BuySelectedWeapon())
            {
                return false;
            }

            return SelectSelectedWeapon();
        }

        public bool IsPurchased(WeaponData weaponData)
        {
            return weaponData != null &&
                   (weaponData.Cost <= 0 ||
                    PlayerPrefs.GetInt(GetPurchasedKey(weaponData), 0) == 1);
        }

        public bool IsEquipped(WeaponData weaponData)
        {
            return weaponData != null &&
                   PlayerPrefs.GetString(WeaponSaveKeys.EquippedWeapon, string.Empty) == weaponData.SaveId;
        }

        public bool CanBuy(WeaponData weaponData)
        {
            if (weaponData == null || IsPurchased(weaponData))
            {
                return false;
            }

            return weaponData.Cost <= 0 ||
                   (_currencyService != null &&
                    _currencyService.CanPurchase(_purchaseCurrencyId, weaponData.Cost));
        }

        public void OnBuyButtonClicked()
        {
            BuySelectedWeapon();
        }

        public void OnSelectButtonClicked()
        {
            SelectSelectedWeapon();
        }

        private void BuildCards()
        {
            ResolveCardPrefabFromContainer();
            ClearContainer();
            _cards.Clear();

            if (_weaponCardPrefab == null || _iapItemContainer == null)
            {
                return;
            }

            for (var i = 0; i < WeaponItems.Count; i++)
            {
                var weaponData = WeaponItems[i];

                if (weaponData == null)
                {
                    continue;
                }

                var card = Instantiate(_weaponCardPrefab, _iapItemContainer);

                if (card == null)
                {
                    continue;
                }

                card.gameObject.SetActive(true);
                card.Setup(weaponData, OnCardClicked);
                _cards.Add(card);
            }
        }

        private void ShowInitialWeapon()
        {
            var equippedWeaponId = PlayerPrefs.GetString(WeaponSaveKeys.EquippedWeapon, string.Empty);

            if (!string.IsNullOrWhiteSpace(equippedWeaponId))
            {
                for (var i = 0; i < WeaponItems.Count; i++)
                {
                    if (WeaponItems[i] != null &&
                        WeaponItems[i].SaveId == equippedWeaponId &&
                        IsPurchased(WeaponItems[i]))
                    {
                        Show(i);
                        return;
                    }
                }
            }

            Show(_startIndex);
        }

        private void RefreshSelectedWeaponState()
        {
            var weaponData = SelectedWeapon;
            var isPurchased = IsPurchased(weaponData);

            RefreshSelectedWeaponInfo(weaponData);
            RefreshCardSelection(weaponData);

            if (_buyButton != null)
            {
                _buyButton.gameObject.SetActive(weaponData != null);
                _costGroup.SetActive(weaponData != null);
                _buyButton.interactable = weaponData != null &&
                                          !isPurchased &&
                                          CanBuy(weaponData);
            }

            RefreshBuyButtonVisual(weaponData != null && !isPurchased);

            if (_selectButton != null)
            {
                _selectButton.gameObject.SetActive(weaponData != null);
                _selectButton.interactable = weaponData != null &&
                                             isPurchased &&
                                             !IsEquipped(weaponData);
            }
        }

        private void RefreshSelectedWeaponInfo(WeaponData weaponData)
        {
            if (_weaponNameText != null)
            {
                _weaponNameText.text = weaponData != null ? weaponData.DisplayName : string.Empty;
            }

            if (_weaponDescriptionText != null)
            {
                _weaponDescriptionText.text = weaponData != null ? weaponData.WeaponDescription : string.Empty;
            }

            if (_rareText != null)
            {
                _rareText.text = weaponData != null ? weaponData.RarityText : string.Empty;
            }

            if (_rareLabel != null && weaponData != null)
            {
                _rareLabel.color = weaponData.RarityColor;
            }

            if (_costAmountText != null)
            {
                _costAmountText.text = weaponData != null ? weaponData.Cost.ToString() : string.Empty;
            }
        }

        private void RefreshCardSelection(WeaponData selectedWeapon)
        {
            for (var i = 0; i < _cards.Count; i++)
            {
                if (_cards[i] != null)
                {
                    _cards[i].SetSelected(_cards[i].WeaponData == selectedWeapon);
                }
            }
        }

        private void OnCardClicked(WeaponCardUI card)
        {
            if (card == null)
            {
                return;
            }

            Show(card.WeaponData);
        }

        private void OnCloseButtonClicked()
        {
            Disappear();
        }

        private void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            _isInitialized = true;
            BuildCards();
            ShowInitialWeapon();
        }

        private void ResolveUiReferences()
        {
            ResolveBuyButtonVisualReferences();
        }

        private void ResolveBuyButtonVisualReferences()
        {
            if (_buyButton == null)
            {
                return;
            }

            if (_buyButtonImage == null)
            {
                _buyButtonImage = _buyButton.GetComponent<Image>();
            }

            if (_buyButtonText != null)
            {
                return;
            }

            if (_costAmountText != null &&
                _costAmountText.transform.IsChildOf(_buyButton.transform))
            {
                _buyButtonText = _costAmountText;
                return;
            }

            _buyButtonText = _buyButton.GetComponentInChildren<TMP_Text>(true);
        }

        private void RefreshBuyButtonVisual(bool visible)
        {
            if (_buyButtonImage != null)
            {
                _buyButtonImage.enabled = visible;
            }

            _costGroup.SetActive(false);
        }

        private void InjectCloseButton()
        {
            if (_resolver == null || _closeButton == null)
            {
                return;
            }

            _resolver.Inject(_closeButton);
        }

        private void RegisterButtons()
        {
            if (_buyButton != null)
            {
                _buyButton.onClick.AddListener(OnBuyButtonClicked);
            }

            if (_selectButton != null)
            {
                _selectButton.onClick.AddListener(OnSelectButtonClicked);
            }

            if (_closeButton != null && _closeButton.Button != null)
            {
                _closeButton.Button.onClick.AddListener(OnCloseButtonClicked);
            }
        }

        private void UnregisterButtons()
        {
            if (_buyButton != null)
            {
                _buyButton.onClick.RemoveListener(OnBuyButtonClicked);
            }

            if (_selectButton != null)
            {
                _selectButton.onClick.RemoveListener(OnSelectButtonClicked);
            }

            if (_closeButton != null && _closeButton.Button != null)
            {
                _closeButton.Button.onClick.RemoveListener(OnCloseButtonClicked);
            }
        }

        private void MarkPurchased(WeaponData weaponData)
        {
            if (weaponData == null)
            {
                return;
            }

            PlayerPrefs.SetInt(GetPurchasedKey(weaponData), 1);
            PlayerPrefs.Save();
        }

        private string GetPurchasedKey(WeaponData weaponData)
        {
            return $"{WeaponSaveKeys.PurchasedWeaponPrefix}{weaponData.SaveId}";
        }

        private bool TryFindValidIndex(int startIndex, out int validIndex)
        {
            validIndex = -1;

            if (WeaponItems.Count == 0)
            {
                return false;
            }

            var index = WrapIndex(startIndex);

            for (var i = 0; i < WeaponItems.Count; i++)
            {
                if (IsValidWeaponIndex(index))
                {
                    validIndex = index;
                    return true;
                }

                index = WrapIndex(index + 1);
            }

            return false;
        }

        private bool IsValidWeaponIndex(int index)
        {
            return index >= 0 &&
                   index < WeaponItems.Count &&
                   WeaponItems[index] != null &&
                   WeaponItems[index].WeaponPrefab != null;
        }

        private int WrapIndex(int index)
        {
            if (WeaponItems.Count == 0)
            {
                return -1;
            }

            index %= WeaponItems.Count;

            if (index < 0)
            {
                index += WeaponItems.Count;
            }

            return index;
        }

        private void ResolveCardPrefabFromContainer()
        {
            if (_weaponCardPrefab != null || _iapItemContainer == null)
            {
                return;
            }

            _weaponCardPrefab = _iapItemContainer.GetComponentInChildren<WeaponCardUI>(true);
        }

        private void ClearContainer()
        {
            if (_iapItemContainer == null)
            {
                return;
            }

            for (var i = _iapItemContainer.childCount - 1; i >= 0; i--)
            {
                var child = _iapItemContainer.GetChild(i);

                if (child != null)
                {
                    child.gameObject.SetActive(false);
                    Destroy(child.gameObject);
                }
            }
        }
    }
}
