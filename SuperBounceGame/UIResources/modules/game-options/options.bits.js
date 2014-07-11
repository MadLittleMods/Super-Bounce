define(['module', 'jquery', 'jquery-utility'], function(module, $, $utility) {

	function loadOptions(restrictTo)
	{
		// Set the input to the last saved value
		engine.call('GetMasterVolume', 'Music').then(function(volume) {
			$(restrictTo).filterFind('input[type="range"].master_music_volume').attr('value', volume);
		});
		engine.call('GetMasterVolume', 'SoundEffect').then(function(volume) {
			$(restrictTo).filterFind('input[type="range"].master_soundeffect_volume').attr('value', volume);
		});
		engine.call('GetMouseSensitivity').then(function(sensitivity) {
			$(restrictTo).filterFind('input[type="range"].mouse_sensitivity').attr('value', sensitivity);
		});
		engine.call('GetMovemementInputSmoothing').then(function(inputSmoothing) {
			$(restrictTo).filterFind('input[type="range"].movement_input_smoothing').attr('value', inputSmoothing);
		});
	}

	return {

		bind: function(restrictTo) {
			// Set restricTo default
			restrictTo = typeof restrictTo !== 'undefined' ? restrictTo : $(document);

			$(document).ready(function() {
				// Binds the loadOptions (js) function to this call in C#:
				// m_View.View.TriggerEvent("updateOptions");
				engine.off('updateOptions', loadOptions, module.id);
				engine.on('updateOptions', loadOptions, module.id);

				// Init all the options
				loadOptions(restrictTo);


				// Bind the master volume controls
				$(restrictTo).filterFind('input[type="range"].master_music_volume').change(function () {
					//console.log("GUI Change master music volume: " + $(this).val());
					engine.call('GUISetMasterVolume', 'music', parseFloat($(this).val()));
				});
				$(restrictTo).filterFind('input[type="range"].master_soundeffect_volume').change(function () {
					engine.call('GUISetMasterVolume', 'soundeffect', parseFloat($(this).val()));
				});

				// Mouse sensitivity
				$(restrictTo).filterFind('input[type="range"].mouse_sensitivity').change(function () {
					engine.call('SetMouseSensitivity', parseFloat($(this).val()));
				});

				// Movement Input Smoothing
				$(restrictTo).filterFind('input[type="range"].movement_input_smoothing').change(function () {
					engine.call('SetMovemementInputSmoothing', parseFloat($(this).val()));
				});

				
			});

		}
	};

});