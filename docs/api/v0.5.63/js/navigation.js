$(document).ready(function() {
  var scroll = function(selector) {
    var currentItem = $(selector + ' .current');
    
    if (currentItem)
      $(selector + ' div.scroll').scrollTo(currentItem);
  };
  
  scroll('#namespaces');
  scroll('#types');
});