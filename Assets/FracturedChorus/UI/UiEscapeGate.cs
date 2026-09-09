using System.Collections.Generic;
using UnityEngine;

namespace FracturedChorus.UI
{
    /// <summary>
    /// Ngăn xếp các panel modal đang mở, dùng để phân xử ai được ăn phím ESC.
    /// Không có nó thì confirm dialog, panel save và status menu toàn cục cùng phản ứng
    /// trong một frame — bấm ESC một cái đóng sập cả ba lớp.
    /// </summary>
    public static class UiEscapeGate
    {
        private static readonly List<Object> Holders = new List<Object>();
        private static int s_consumedFrame = -1;

        /// <summary>True khi có panel đang giữ ESC, tức là lớp nền không được xử lý phím này.</summary>
        public static bool IsBlocked
        {
            get
            {
                Prune();
                return Holders.Count > 0;
            }
        }

        public static void Push(Object owner)
        {
            if (owner == null)
            {
                return;
            }

            Holders.Remove(owner);
            Holders.Add(owner);
        }

        public static void Pop(Object owner)
        {
            if (owner == null)
            {
                return;
            }

            Holders.Remove(owner);
            Prune();
        }

        /// <summary>
        /// Xin quyền xử lý lần bấm ESC của frame hiện tại. Chỉ panel trên cùng được chấp nhận,
        /// và mỗi frame chỉ một panel được nhận — panel bên dưới không ăn ké khi lớp trên vừa đóng.
        /// </summary>
        public static bool TryConsume(Object owner)
        {
            if (owner == null || s_consumedFrame == Time.frameCount)
            {
                return false;
            }

            Prune();
            if (Holders.Count == 0 || Holders[Holders.Count - 1] != owner)
            {
                return false;
            }

            s_consumedFrame = Time.frameCount;
            return true;
        }

        /// <summary>Đánh dấu ESC của frame này đã được xử lý, cho panel không nằm trong ngăn xếp.</summary>
        public static bool TryConsumeBackground()
        {
            if (s_consumedFrame == Time.frameCount || IsBlocked)
            {
                return false;
            }

            s_consumedFrame = Time.frameCount;
            return true;
        }

        /// <summary>Panel bị Destroy khi đổi scene để lại entry rỗng — dọn để cổng không kẹt vĩnh viễn.</summary>
        private static void Prune()
        {
            for (var i = Holders.Count - 1; i >= 0; i--)
            {
                if (Holders[i] == null)
                {
                    Holders.RemoveAt(i);
                }
            }
        }
    }
}
