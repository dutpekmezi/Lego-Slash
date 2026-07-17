using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TrashRush.Game
{
    public sealed class WeaponRenderController : MonoBehaviour
    {
        [SerializeField] private WeaponDatas _weaponDatas;
        [SerializeField] private Transform _weaponPoint;
        [SerializeField] private WeaponCardUI _weaponCardPrefab;
        [SerializeField] private Transform _iapItemContainer;
        [Header("Selected Weapon Info")]
        [SerializeField] private TMP_Text _weaponNameLabel;
        [SerializeField] private TMP_Text _weaponDescriptionLabel;
        [SerializeField] private TMP_Text _rarityLabel;
        [SerializeField] private Image _rarityBackground;
        [SerializeField, Min(0)] private int _startIndex;

        private readonly List<WeaponCardUI> _cards = new();
        private GameObject _currentWeaponInstance;
        private int _currentIndex = -1;

        private IReadOnlyList<WeaponData> Weapons =>
            _weaponDatas != null ? _weaponDatas.Items : System.Array.Empty<WeaponData>();

        private void Awake()
        {
            BuildCards();
            ShowInitialWeapon();
        }

        private void OnDestroy()
        {
            DestroyCurrentWeapon();
        }

        public void Show(int index)
        {
            if (Weapons.Count == 0)
            {
                _currentIndex = -1;
                DestroyCurrentWeapon();
                return;
            }

            index = WrapIndex(index);

            if (!IsValidWeaponIndex(index) &&
                !TryFindValidIndex(index, out index))
            {
                _currentIndex = -1;
                DestroyCurrentWeapon();
                return;
            }

            _currentIndex = index;
            RenderCurrentWeapon();
            RefreshSelectedWeaponInfo();
            RefreshCardSelection();
        }

        public void Show(WeaponData weaponData)
        {
            if (weaponData == null)
            {
                return;
            }

            for (var i = 0; i < Weapons.Count; i++)
            {
                if (Weapons[i] == weaponData)
                {
                    Show(i);
                    return;
                }
            }
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

            for (var i = 0; i < Weapons.Count; i++)
            {
                var weaponData = Weapons[i];

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
            Show(_startIndex);
        }

        private void RenderCurrentWeapon()
        {
            DestroyCurrentWeapon();

            var weaponData = IsValidWeaponIndex(_currentIndex)
                ? Weapons[_currentIndex]
                : null;
            var weaponPrefab = weaponData != null ? weaponData.WeaponPrefab : null;

            if (_weaponPoint == null || weaponPrefab == null)
            {
                return;
            }

            _currentWeaponInstance = Instantiate(weaponPrefab, _weaponPoint);
            _currentWeaponInstance.transform.localPosition = Vector3.zero;
            _currentWeaponInstance.transform.localRotation = weaponPrefab.transform.localRotation;
            _currentWeaponInstance.transform.localScale = weaponPrefab.transform.localScale;
        }

        private void RefreshSelectedWeaponInfo()
        {
            var weaponData = IsValidWeaponIndex(_currentIndex)
                ? Weapons[_currentIndex]
                : null;

            if (_weaponNameLabel != null)
            {
                _weaponNameLabel.text = weaponData != null ? weaponData.DisplayName : string.Empty;
            }

            if (_weaponDescriptionLabel != null)
            {
                _weaponDescriptionLabel.text = weaponData != null ? weaponData.WeaponDescription : string.Empty;
            }

            if (_rarityLabel != null)
            {
                _rarityLabel.text = weaponData != null ? weaponData.RarityText : string.Empty;
            }

            if (_rarityBackground != null && weaponData != null)
            {
                _rarityBackground.color = weaponData.RarityColor;
            }
        }

        private void RefreshCardSelection()
        {
            var currentWeaponData = IsValidWeaponIndex(_currentIndex)
                ? Weapons[_currentIndex]
                : null;

            for (var i = 0; i < _cards.Count; i++)
            {
                if (_cards[i] != null)
                {
                    _cards[i].SetSelected(_cards[i].WeaponData == currentWeaponData);
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

        private void DestroyCurrentWeapon()
        {
            if (_currentWeaponInstance == null)
            {
                return;
            }

            Destroy(_currentWeaponInstance);
            _currentWeaponInstance = null;
        }

        private bool TryFindValidIndex(int startIndex, out int validIndex)
        {
            validIndex = -1;

            if (Weapons.Count == 0)
            {
                return false;
            }

            var index = WrapIndex(startIndex);

            for (var i = 0; i < Weapons.Count; i++)
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
                   index < Weapons.Count &&
                   Weapons[index] != null &&
                   Weapons[index].WeaponPrefab != null;
        }

        private int WrapIndex(int index)
        {
            if (Weapons.Count == 0)
            {
                return -1;
            }

            index %= Weapons.Count;

            if (index < 0)
            {
                index += Weapons.Count;
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
