$(document).ready(function() {
	//Navigation Menu Slider
	$('#toggle-menu').on('click',function(e){
		e.preventDefault();
		$('body').toggleClass('nav-expanded');
	});
	$('#close').on('click',function(e){
		e.preventDefault();
		$('body').removeClass('nav-expanded');
	});	
	  $('#toggle-menu').click(function(){
		$(this).toggleClass('menu-is-active')
	});
	
	
});