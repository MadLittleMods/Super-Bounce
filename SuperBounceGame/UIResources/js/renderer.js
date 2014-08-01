/*
 * Available functions:
 *	renderer.replace(...options...)
 *	renderer.append(...options...)
 *	renderer.prepend(...options...)
 *	renderer.before(...options...)
 *	renderer.after(...options...)
 *
 * Options: 
 * 	module: The RequireJS module to render. 
 * 		The module should return { template: function(context) { }, attach: function(options, elements) { } }
 * 	context: The template context to pass
 * 	attachOptons: Any specific info you can pass into the the `attach` callback
 * 	element: Element to to render the module on/to
 * 	callback: function to call after everything has happened
 * 	localRequire: An optional local reference of RequireJS. If supplied, we will use this to require in dependencies. This is good for not including duplicate dependencies
 *
 * Syntax: 
 *	renderer.replace('module-lobby', { isServer: true }, {}, $('.main-ui-layout-body.server-lobby'), function(attachReturn) {
 *		console.log('lobby template rendered');
 *	});
 *
 * module-lobby.js
 *	define(['require', 'hbs!./score-box/score-box', 'css!./score-box/score-box'], function(require, tmpl, css) {
 *		return {
 *			template: tmpl,
 *			attach: function(options, elements) {
 *				// You can do binding to the `elements` you just added
 *				// even return some methods
 *				require(['./score-box/score.bits'], function(scoreBox) {
 *					scoreBox.bind(elements);
 *				});
 *			}
 *		};
 *	});
 */

var renderer = (function() {
	function genericRender(addTemplateAndReturnElements) {
		return function(module, context, attachOptions, element, callback, localRequire) {
			// Set require default
			// You might want to pass your own require so you know what assets that are already added
			require = typeof localRequire !== 'undefined' ? localRequire : require;

			require([module], function(cpt) {
				// Add the template html into the parent
				var elements = addTemplateAndReturnElements(cpt.template(context), element);
				//console.log(elements);

				// Call the callback
				callback(cpt.attach(attachOptions, elements));
			});
		}
	}

	return {
		replace: genericRender(function(html, element){
			return $(element).html(html).children();
		}),
		append: genericRender(function(html, element){
			return $(html).appendTo(element);
		}),
		prepend: genericRender(function(html, element){
			return $(html).prependTo(element);
		}),
		before: genericRender(function(html, element){
			return $(html).insertBefore(element);
		}),
		after: genericRender(function(html, element){
			return $(html).insertAfter(element);
		})
	}
})();

// Turn this into a RequireJS module
define(function() {
	return renderer;
});