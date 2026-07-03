using UnityEngine;

namespace FracturedChorus.UI
{
    /// <summary>
    /// Đọc kích thước thật của RectTransform trong scene thay vì hardcode trong code.
    /// Quy ước: nếu object đã được chỉnh size trong Hierarchy (cả 2 chiều &gt; 0) thì dùng số đó;
    /// nếu chưa (0×0, ví dụ object stretch full) thì mới dùng fallback.
    /// </summary>
    public static class RectSizeUtil
    {
        /// <summary>
        /// Object đã được tác giả chỉnh kích thước trong scene chưa?
        /// True khi sizeDelta cả 2 chiều &gt; 0, hoặc object stretch nhưng rect hiển thị &gt; 0.
        /// Dùng để runtime KHÔNG ghi đè hình học đã set trong Hierarchy.
        /// </summary>
        public static bool IsAuthored(RectTransform rect)
        {
            if (rect == null)
            {
                return false;
            }

            var size = rect.sizeDelta;
            if (size.x > 0f && size.y > 0f)
            {
                return true;
            }

            var worldSize = rect.rect.size;
            return worldSize.x > 0f && worldSize.y > 0f;
        }

        /// <summary>Trả về sizeDelta scene nếu hợp lệ, ngược lại dùng <paramref name="fallback"/>.</summary>
        public static Vector2 ResolveSize(RectTransform rect, Vector2 fallback)
        {
            if (rect != null)
            {
                var size = rect.sizeDelta;
                if (size.x > 0f && size.y > 0f)
                {
                    return size;
                }

                // Object stretch (anchors khác nhau) → lấy kích thước hiển thị thực tế.
                var worldSize = rect.rect.size;
                if (worldSize.x > 0f && worldSize.y > 0f)
                {
                    return worldSize;
                }
            }

            return fallback;
        }

        /// <summary>Scale tay được đặt trong scene (fallback (1,1,1)).</summary>
        public static Vector3 ResolveScale(Transform transform)
        {
            return transform != null ? transform.localScale : Vector3.one;
        }

        /// <summary>Cạnh nhỏ hơn của size đã resolve — dùng cho layout vuông (radial, badge).</summary>
        public static float ResolveMinExtent(RectTransform rect, float fallback)
        {
            var size = ResolveSize(rect, new Vector2(fallback, fallback));
            return Mathf.Min(size.x, size.y);
        }
    }
}
