using System;
using System.Collections.Generic;
using System.Linq;

namespace bts.utils {
   // See http://xmailserver.org/diff2.pdf
   public static class Diff {

      public struct Item {
         public int StartLeft;
         public int StartRight;
         public int deletedLeft;
         public int insertedRight;
         public override string ToString() {
            return $"{StartLeft} {StartRight} {deletedLeft} {insertedRight}";
         }
      }
      struct ShortestMiddleSnake {
         public int x, y;
      }

      // Represents one collection
      class DiffData {
         public int length;
         public int[] data;
         public bool[] modified;
         public DiffData(int[] data) {
            this.data = data;
            length = data.Length;
            modified = new bool[length + 2];
         }
      }

      static int[] diff_codes<T>(IList<T> input, Dictionary<T, int> cache) {
         var length = input.Count;
         int[] codes = new int[length];
         int lastCode = cache.Count;

         for (int x = 0; x < length; ++x) {
            var item = input[x];
            if (cache.TryGetValue(item, out int temp)) {
               codes[x] = temp;
            }
            else {
               lastCode++;
               cache[item] = lastCode;
               codes[x] = lastCode;
            }
         }
         return codes;
      }

      public static IEnumerable<Item> diff<T>(IEnumerable<T> left, IEnumerable<T> right, IEqualityComparer<T> comp = null) {
         var leftList = left as IList<T> ?? left.ToList();
         var rightList = right as IList<T> ?? right.ToList();

         Dictionary<T, int> cache = new Dictionary<T, int>(comp ?? EqualityComparer<T>.Default);
         var leftData = new DiffData(diff_codes(leftList, cache));
         var rightData = new DiffData(diff_codes(rightList, cache));

         int MAX = leftList.Count + rightList.Count + 1;
         int[] downVector = new int[2 * MAX + 2];
         int[] upVector = new int[2 * MAX + 2];

         lcs(leftData, 0, leftData.length, rightData, 0, rightData.length, downVector, upVector);
         return createDiffs(leftData, rightData);
      }

      //public struct Change {
      //}
      //public static IEnumerable<

      static IEnumerable<Item> createDiffs(DiffData dataLeft, DiffData dataRight) {
         int startLeft, startRight;
         int itemLeft = 0, ItemRight = 0;
         while(itemLeft < dataLeft.length || ItemRight < dataRight.length) {
            if ((itemLeft < dataLeft.length) && (!dataLeft.modified[itemLeft]) &&
                (ItemRight < dataRight.length) && (!dataRight.modified[ItemRight])) {
               // Equal entries
               itemLeft++;
               ItemRight++;
            }
            else {
               startLeft = itemLeft;
               startRight = ItemRight;
               while (itemLeft < dataLeft.length && (ItemRight >= dataRight.length || dataLeft.modified[itemLeft])) {
                  itemLeft++;
               }
               while (ItemRight < dataRight.length && (itemLeft >= dataLeft.length || dataRight.modified[ItemRight])) {
                  ItemRight++;
               }
               if ((startLeft < itemLeft) || (startRight < ItemRight)) {
                  yield return new Item() {
                     StartLeft = startLeft,
                     StartRight = startRight,
                     deletedLeft = itemLeft - startLeft,
                     insertedRight = ItemRight - startRight
                  };
               }
            }
         }
      }

      // Longest common substring algorithm
      private static void lcs(DiffData dataLeft, int lowerLeft, int upperLeft, DiffData dataRight, int lowerRight, int upperRight, int[] downVector, int[] upVector) {
         // Fast walkthrough equal lines at the start
         while (lowerLeft < upperLeft && 
               lowerRight < upperRight && 
               dataLeft.data[lowerLeft] == dataRight.data[lowerRight]) {
            lowerLeft++; lowerRight++;
         }

         // Fast walkthrough equal lines at the end
         while (lowerLeft < upperLeft && 
               lowerRight < upperRight && 
               dataLeft.data[upperLeft - 1] == dataRight.data[upperRight - 1]) {
            --upperLeft; --upperRight;
         }

         if (lowerLeft == upperLeft) {
            // mark as inserted lines.
            while (lowerRight < upperRight) {
               dataRight.modified[lowerRight++] = true;
            }

         }
         else if (lowerRight == upperRight) {
            // mark as deleted lines.
            while (lowerLeft < upperLeft) {
               dataLeft.modified[lowerLeft++] = true;
            }

         }
         else {
            // Find the middle snake and length of an optimal path for A and B
            var smsrd = sms(dataLeft, lowerLeft, upperLeft, dataRight, lowerRight, upperRight, downVector, upVector);

            // The path is from LowerX to (x,y) and (x,y) to UpperX
            lcs(dataLeft, lowerLeft, smsrd.x, dataRight, lowerRight, smsrd.y, downVector, upVector);
            lcs(dataLeft, smsrd.x, upperLeft, dataRight, smsrd.y, upperRight, downVector, upVector);
         }
      }

      static bool isOdd(int x) { return (x & 1) != 0; }

      // Shortest Middle Snake algorithm
      static ShortestMiddleSnake sms(
         DiffData dataLeft, int lowerLeft, int upperLeft, 
         DiffData dataRight, int lowerRight, int upperRight, 
         int[] downVector, int[] upVector) {

         int MAX = dataLeft.length + dataRight.length + 1;
         int downk = lowerLeft - lowerRight;
         int upk = upperLeft - upperRight;
         int delta = (upperLeft - lowerLeft) - (upperRight - lowerRight);
         bool delta_is_odd = isOdd(delta);
         bool delta_is_even = !delta_is_odd;
         int downOffset = MAX - downk;
         int upOffset = MAX - upk;
         int maxd = ((upperLeft - lowerLeft + upperRight - lowerRight) / 2) + 1;

         downVector[downOffset + downk + 1] = lowerLeft;
         upVector[upOffset + upk - 1] = upperLeft;

         for (int d = 0; d <= maxd; ++d) {
            // Extend the forward path
            for (int k = downk - d; k <= downk + d; k += 2) {
               // Find the starting point
               int x, y;
               if (k == downk - d) {
                  x = downVector[downOffset + k + 1];
               }
               else {
                  x = downVector[downOffset + k - 1] + 1;
                  if ((k < downk + d) && (downVector[downOffset + k + 1] >= x)) {
                     x = downVector[downOffset + k + 1];
                  }
               }
               y = x - k;

               // find the end of the furthest reaching forward d-path in diagonal k
               while ((x < upperLeft) && (y < upperRight) && (dataLeft.data[x] == dataRight.data[y])) {
                  x++; y++;
               }
               downVector[downOffset + k] = x;

               // detect overlap
               if (delta_is_odd && (upk - d < k) && (k < upk + d)) {
                  if (upVector[upOffset + k] <= downVector[downOffset + k]) {
                     return new ShortestMiddleSnake() {
                        x = downVector[downOffset + k],
                        y = downVector[downOffset + k] - k
                     };
                  }
               }
            }
            // extend reverse path
            for (int k = upk - d; k <= upk + d; k += 2) {
               // find the starting point
               int x, y;
               if (k == upk + d) {
                  x = upVector[upOffset + k - 1]; // up
               }
               else {
                  x = upVector[upOffset + k + 1] - 1; // left
                  if ((k > upk - d) && (upVector[upOffset + k - 1] < x)) {
                     x = upVector[upOffset + k - 1]; // up
                  }
               }
               y = x - k;

               while ((x > lowerLeft) && (y > lowerRight) && (dataLeft.data[x - 1] == dataRight.data[y - 1])) {
                  x--; y--; // diagonal
               }
               upVector[upOffset + k] = x;

               // detect overlap
               if (delta_is_even && (downk - d <= k) && (k <= downk + d)) {
                  if (upVector[upOffset + k] <= downVector[downOffset + k]) {
                     return new ShortestMiddleSnake() {
                        x = downVector[downOffset + k],
                        y = downVector[downOffset + k] - k
                     };
                  }
               }
            }
         }

         throw new ApplicationException("Unexpected code path");
      }
   }
}