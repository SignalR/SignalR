$(document).ready(function() {
    $('div.example').each(function(i, div) {
        var a = $('a', div);
        var pre = $('pre', div);

        a.pre = pre;
        a.preVisible = false;
        pre.hide();
        a.click(function() {
            if (a.preVisible) {
                a.pre.hide();
                a.text('Show Example');
                a.preVisible = false;
            } else {
                a.pre.show();
                a.text('Hide Example');
                a.preVisible = true;
            }
        });
    });
});