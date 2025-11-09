using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Expo.Data;

namespace Expo.UI
{
    /// <summary>
    /// Individual table button in the table selection menu.
    /// Displays table info and handles click events.
    /// </summary>
    public class TableButtonUI : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TextMeshProUGUI tableNumberText;
        [SerializeField] private TextMeshProUGUI tableInfoText;

        private int _tableNumber;
        private Action<int> _onClicked;

        private void Awake()
        {
            if (button != null)
            {
                button.onClick.AddListener(OnButtonClicked);
            }
        }

        /// <summary>
        /// Initializes the button with table data.
        /// </summary>
        public void Init(TableData table, Action<int> onClicked)
        {
            _tableNumber = table.TableNumber;
            _onClicked = onClicked;

            // Set table number text
            if (tableNumberText != null)
            {
                tableNumberText.text = $"Table {table.TableNumber}";
            }

            // Set additional info (ticket ID and party size)
            if (tableInfoText != null)
            {
                // string infoText = $"{table.PartySize} guests";
                // if (table.CurrentTicketId.HasValue)
                // {
                //     infoText += $"\nTicket #{table.CurrentTicketId.Value}";
                // }
                // tableInfoText.text = infoText;
            }

            // Enable the button
            if (button != null)
            {
                button.interactable = true;
            }
        }

        private void OnButtonClicked()
        {
            _onClicked?.Invoke(_tableNumber);
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(OnButtonClicked);
            }
        }
    }
}
