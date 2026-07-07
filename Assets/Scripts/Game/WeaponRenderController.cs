using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TrashRush.Game
{
    public sealed class WeaponRenderController : MonoBehaviour
    {
        [SerializeField] private WeaponDatas _weaponDatas;
        [SerializeField] private Transform _weaponPoint;
        [SerializeField] private Transform _weaponsPageRoot;
        [SerializeField] private Button _leftArrowButton;
        [SerializeField] private Button _rightArrowButton;
        [SerializeField, Min(0)] private int _startIndex;
        [SerializeField] private bool _skipEmptyWeaponPrefabs = true;

        private GameObject _currentWeaponInstance;
        private int _currentIndex = -1;

        private IReadOnlyList<WeaponData> Weapons =>
            _weaponDatas != null ? _weaponDatas.Items : System.Array.Empty<WeaponData>();

        private void Awake()
        {
            ResolveArrowButtons();
            RegisterArrowButtons();
            ShowInitialWeapon();
        }

        private void OnDestroy()
        {
            UnregisterArrowButtons();
            DestroyCurrentWeapon();
        }

        public void ShowNext()
        {
            ShowByDirection(1);
        }

        public void ShowPrevious()
        {
            ShowByDirection(-1);
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

            if (_skipEmptyWeaponPrefabs &&
                !TryFindValidIndex(index, 1, out index))
            {
                _currentIndex = -1;
                DestroyCurrentWeapon();
                return;
            }

            _currentIndex = index;
            RenderCurrentWeapon();
        }

        private void ShowInitialWeapon()
        {
            Show(_startIndex);
        }

        private void ShowByDirection(int direction)
        {
            if (Weapons.Count == 0)
            {
                DestroyCurrentWeapon();
                return;
            }

            var startIndex = _currentIndex >= 0
                ? _currentIndex + direction
                : _startIndex;

            startIndex = WrapIndex(startIndex);

            if (_skipEmptyWeaponPrefabs &&
                !TryFindValidIndex(startIndex, direction, out startIndex))
            {
                DestroyCurrentWeapon();
                return;
            }

            Show(startIndex);
        }

        private void RenderCurrentWeapon()
        {
            DestroyCurrentWeapon();

            if (_weaponPoint == null ||
                _currentIndex < 0 ||
                _currentIndex >= Weapons.Count)
            {
                return;
            }

            var weaponData = Weapons[_currentIndex];
            var weaponPrefab = weaponData != null ? weaponData.WeaponPrefab : null;

            if (weaponPrefab == null)
            {
                return;
            }

            _currentWeaponInstance = Instantiate(weaponPrefab, _weaponPoint);
            _currentWeaponInstance.transform.localPosition = Vector3.zero;
            _currentWeaponInstance.transform.localRotation = weaponPrefab.transform.localRotation;
            _currentWeaponInstance.transform.localScale = weaponPrefab.transform.localScale;
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

        private bool TryFindValidIndex(int startIndex, int direction, out int validIndex)
        {
            validIndex = -1;

            if (Weapons.Count == 0)
            {
                return false;
            }

            direction = direction >= 0 ? 1 : -1;
            var index = WrapIndex(startIndex);

            for (var i = 0; i < Weapons.Count; i++)
            {
                var weaponData = Weapons[index];

                if (weaponData != null && weaponData.WeaponPrefab != null)
                {
                    validIndex = index;
                    return true;
                }

                index = WrapIndex(index + direction);
            }

            return false;
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

        private void ResolveArrowButtons()
        {
            if (_weaponsPageRoot == null)
            {
                return;
            }

            var buttons = _weaponsPageRoot.GetComponentsInChildren<Button>(true);

            for (var i = 0; i < buttons.Length; i++)
            {
                var button = buttons[i];

                if (button == null)
                {
                    continue;
                }

                if (_leftArrowButton == null && button.name == "LeftArrow")
                {
                    _leftArrowButton = button;
                    continue;
                }

                if (_rightArrowButton == null && button.name == "RightArrow")
                {
                    _rightArrowButton = button;
                }
            }
        }

        private void RegisterArrowButtons()
        {
            if (_leftArrowButton != null)
            {
                _leftArrowButton.onClick.AddListener(ShowPrevious);
            }

            if (_rightArrowButton != null)
            {
                _rightArrowButton.onClick.AddListener(ShowNext);
            }
        }

        private void UnregisterArrowButtons()
        {
            if (_leftArrowButton != null)
            {
                _leftArrowButton.onClick.RemoveListener(ShowPrevious);
            }

            if (_rightArrowButton != null)
            {
                _rightArrowButton.onClick.RemoveListener(ShowNext);
            }
        }
    }
}
