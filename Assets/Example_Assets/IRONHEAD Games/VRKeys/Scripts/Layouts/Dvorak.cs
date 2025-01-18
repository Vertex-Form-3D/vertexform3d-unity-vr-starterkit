/**
 * Copyright (c) 2017 The Campfire Union Inc - All Rights Reserved.
 *
 * Licensed under the MIT license. See LICENSE file in the project root for
 * full license information.
 *
 * Email:   info@campfireunion.com
 * Website: https://www.campfireunion.com
 */

namespace VRKeys.Layouts {

	/// <summary>
	/// Dvorak language keyboard.
	/// </summary>
	public class Dvorak : Layout {

		public Dvorak () {
			row1Keys = new string[] { "`", "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "[", "]" };

			row1Shift = new string[] { "~", "!", "@", "#", "$", "%", "^", "&", "*", "(", ")", "{", "}" };

			row2Keys = new string[] { "'", ",", ".", "p", "y", "f", "g", "c", "r", "l", "/", "=", "\\" };

			row2Shift = new string[] { "\"", "<", ">", "P", "Y", "F", "G", "C", "R", "L", "?", "+", "|" };

			row3Keys = new string[] { "a", "o", "e", "u", "i", "d", "h", "t", "n", "s", "-" };

			row3Shift = new string[] { "A", "O", "E", "U", "I", "D", "H", "T", "N", "S", "_" };

			row4Keys = new string[] { ";", "q", "j", "k", "x", "b", "m", "w", "v", "z" };

			row4Shift = new string[] { ":", "Q", "J", "K", "X", "B", "M", "W", "V", "Z" };
		}
	}
}