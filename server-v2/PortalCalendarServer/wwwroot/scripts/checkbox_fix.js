jQuery(document).ready(function ($) {
    $(document).on('submit', 'form', function (e) {
        var $form = $(this);

        $form.find('input:checkbox:not(:checked):not(:disabled)').each(function (index, input) {
            $form.append(
                $('<input />')
                    .attr('type', 'hidden')
                    .attr('name', input.name)
                    .val(0)
            );
        });
    })
});
