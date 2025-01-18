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
	/// French language keyboard (AZERTY). Note that some liberties had to be taken
	/// because of the lack of shift + ctrl combination and diacritic key.
	/// </summary>
	public class French : Layout {

		public French () {
			placeholderMessage = "Appuyez sur les touches pour commencer à taper";

			spaceButtonLabel = "ESPACE";

			enterButtonLabel = "ENTRÉE";

			cancelButtonLabel = "ANNULER";

			shiftButtonLabel = "MAJUSCULE";

			backspaceButtonLabel = "RÉTRO-ESPACE";

			clearButtonLabel = "EFFACER";

			row1Keys = new string[] { "â", "&", "é", "\"", "'", "(", "-", "è", "_", "ç", "à", ")", "=" };

			row1Shift = new string[] { "ê", "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "-", "ë" };

			row2Keys = new string[] { "a", "z", "e", "r", "t", "y", "u", "i", "o", "p", "ô", "$", "*" };

			row2Shift = new string[] { "A", "Z", "E", "R", "T", "Y", "U", "I", "O", "P", "î", "£", "µ" };

			row3Keys = new string[] { "q", "s", "d", "f", "g", "h", "j", "k", "l", "m", "ù" };

			row3Shift = new string[] { "Q", "S", "D", "F", "G", "H", "J", "K", "L", "M", "%" };

			row4Keys = new string[] { "<", "w", "x", "c", "v", "b", "n", ",", ";", ":", "!" };

			row4Shift = new string[] { ">", "W", "X", "C", "V", "B", "N", "?", ".", "/", "û" };

			row1Offset = 0.16f;

			row2Offset = 0.08f;

			row3Offset = 0f;

			row4Offset = 0f;
		}
	}
}