$(function () {

    renderAlerts();

    function renderAlerts() {
        $('.lucene-block').addClass('alert alert-info');
    }

    // //docfx has a hard coded value of 60px in height check for the nav bar
    // //but our nav bar is taller so we need to work around this
    // function fixAutoCollapseBug() {
    //     autoCollapse();
    //     $(window).on('resize', autoCollapse);
    //     $(document).on('click', '.navbar-collapse.in', function (e) {
    //         if ($(e.target).is('a')) {
    //             $(this).collapse('hide');
    //         }
    //     });

    //     function autoCollapse() {
    //         var navbar = $('#autocollapse');
    //         if (navbar.height() === null) {
    //             setTimeout(autoCollapse, 310);
    //         }
    //         navbar.removeClass(collapsed);
    //         if (navbar.height() > 60) {
    //             navbar.addClass(collapsed);
    //         }
    //     }
    // }

})