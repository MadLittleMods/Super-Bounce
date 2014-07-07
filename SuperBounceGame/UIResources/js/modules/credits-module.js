define(['hbs!modules/credits/credits', 'css!../css/base', 'css!../css/icon-font', 'css!modules/main-ui/main-ui'], function(tmpl, basecss, iconcss, maincss) {
	return {
		template: tmpl,
		attach: function(options, elements) {
			// You can do stuff here
			// even return some methods
			/*
			require(['modules/credits/credits.bits'], function(creditmodule) {
				creditmodule.bind(elements);
			});
			*/
		}
	};
});